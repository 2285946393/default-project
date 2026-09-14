// Package openai defines the OpenAI-compatible requests this gateway accepts.
package openai

import (
	"bytes"
	"encoding/json"
	"errors"
	"fmt"
	"io"
	"mime"
	"mime/multipart"
	"net/http"
	"net/textproto"
	"strings"
)

const (
	ModelsPath          = "/v1/models"
	ChatCompletionsPath = "/v1/chat/completions"
	GenerationsPath     = "/v1/images/generations"
	EditsPath           = "/v1/images/edits"

	imageModel               = "gpt-image-2"
	responseFormat           = "b64_json"
	maxJSONBodyBytes   int64 = 1 << 20
	maxChatBodyBytes   int64 = 20 << 20
	maxPromptBytes           = 64 << 10
	maxImageCount            = 10
	maxImageBytes      int64 = 20 << 20
	maxEditBodyBytes         = maxImageCount*maxImageBytes + 2<<20
	maxMultipartMemory       = 8 << 20
)

// Request is a complete, locally constructed request for the upstream API.
// It contains no client-controlled HTTP metadata.
type Request struct {
	Method      string
	Path        string
	ContentType string
	Body        []byte
}

// RequestError is a client-facing validation failure.
type RequestError struct {
	Status  int
	Message string
	Param   string
}

func (e *RequestError) Error() string {
	return e.Message
}

// AsRequestError returns a client-facing request error when err represents one.
func AsRequestError(err error) (*RequestError, bool) {
	var requestErr *RequestError
	if errors.As(err, &requestErr) {
		return requestErr, true
	}
	return nil, false
}

// BuildRequest parses the incoming request and recreates a minimal request for
// the compatible upstream API. Client query parameters and request headers are
// deliberately discarded. Image requests are normalized, while chat payloads
// are preserved so OpenAI-compatible options can reach the upstream API.
func BuildRequest(w http.ResponseWriter, r *http.Request) (Request, error) {
	switch r.URL.Path {
	case ModelsPath:
		if err := requireMethod(r, http.MethodGet); err != nil {
			return Request{}, err
		}
		return Request{Method: http.MethodGet, Path: ModelsPath}, nil
	case ChatCompletionsPath:
		if err := requireMethod(r, http.MethodPost); err != nil {
			return Request{}, err
		}
		return buildChatCompletionRequest(r)
	case GenerationsPath:
		if err := requireMethod(r, http.MethodPost); err != nil {
			return Request{}, err
		}
		return buildGenerationRequest(r)
	case EditsPath:
		if err := requireMethod(r, http.MethodPost); err != nil {
			return Request{}, err
		}
		return buildEditRequest(w, r)
	default:
		return Request{}, &RequestError{Status: http.StatusNotFound, Message: "unsupported route"}
	}
}

func buildChatCompletionRequest(r *http.Request) (Request, error) {
	if !hasMediaType(r, "application/json") {
		return Request{}, &RequestError{
			Status:  http.StatusUnsupportedMediaType,
			Message: "content type must be application/json",
		}
	}

	body, err := readBody(r.Body, maxChatBodyBytes)
	if err != nil {
		return Request{}, err
	}

	var payload map[string]json.RawMessage
	decoder := json.NewDecoder(bytes.NewReader(body))
	if err := decoder.Decode(&payload); err != nil || payload == nil {
		return Request{}, &RequestError{Status: http.StatusBadRequest, Message: "invalid JSON request body"}
	}
	if err := decoder.Decode(&struct{}{}); err != io.EOF {
		return Request{}, &RequestError{Status: http.StatusBadRequest, Message: "request body must contain one JSON object"}
	}

	return Request{
		Method:      http.MethodPost,
		Path:        ChatCompletionsPath,
		ContentType: "application/json",
		Body:        body,
	}, nil
}

func requireMethod(r *http.Request, method string) error {
	if r.Method == method {
		return nil
	}
	return &RequestError{
		Status:  http.StatusMethodNotAllowed,
		Message: fmt.Sprintf("method %s is not allowed for %s", r.Method, r.URL.Path),
	}
}

type generationInput struct {
	Prompt string `json:"prompt"`
	Size   string `json:"size"`
}

type generationPayload struct {
	Model          string `json:"model"`
	Prompt         string `json:"prompt"`
	N              int    `json:"n"`
	Quality        string `json:"quality"`
	Size           string `json:"size,omitempty"`
	ResponseFormat string `json:"response_format"`
}

func buildGenerationRequest(r *http.Request) (Request, error) {
	if !hasMediaType(r, "application/json") {
		return Request{}, &RequestError{
			Status:  http.StatusUnsupportedMediaType,
			Message: "content type must be application/json",
		}
	}

	body, err := readBody(r.Body, maxJSONBodyBytes)
	if err != nil {
		return Request{}, err
	}

	var input generationInput
	decoder := json.NewDecoder(bytes.NewReader(body))
	if err := decoder.Decode(&input); err != nil {
		return Request{}, &RequestError{Status: http.StatusBadRequest, Message: "invalid JSON request body"}
	}
	if err := decoder.Decode(&struct{}{}); err != io.EOF {
		return Request{}, &RequestError{Status: http.StatusBadRequest, Message: "request body must contain one JSON object"}
	}

	prompt, err := validatePrompt(input.Prompt)
	if err != nil {
		return Request{}, err
	}
	payload, err := json.Marshal(generationPayload{
		Model:          imageModel,
		Prompt:         prompt,
		N:              1,
		Quality:        "auto",
		Size:           input.Size,
		ResponseFormat: responseFormat,
	})
	if err != nil {
		return Request{}, fmt.Errorf("encode generation request: %w", err)
	}

	return Request{
		Method:      http.MethodPost,
		Path:        GenerationsPath,
		ContentType: "application/json",
		Body:        payload,
	}, nil
}

func buildEditRequest(w http.ResponseWriter, r *http.Request) (Request, error) {
	if !hasMediaType(r, "multipart/form-data") {
		return Request{}, &RequestError{
			Status:  http.StatusUnsupportedMediaType,
			Message: "content type must be multipart/form-data",
		}
	}

	r.Body = http.MaxBytesReader(w, r.Body, maxEditBodyBytes)
	if err := r.ParseMultipartForm(maxMultipartMemory); err != nil {
		var maxBytesErr *http.MaxBytesError
		if errors.As(err, &maxBytesErr) {
			return Request{}, &RequestError{Status: http.StatusRequestEntityTooLarge, Message: "multipart request body is too large"}
		}
		return Request{}, &RequestError{Status: http.StatusBadRequest, Message: "invalid multipart request body"}
	}
	defer r.MultipartForm.RemoveAll()

	prompt, err := requiredFormValue(r.MultipartForm.Value, "prompt")
	if err != nil {
		return Request{}, err
	}
	prompt, err = validatePrompt(prompt)
	if err != nil {
		return Request{}, err
	}
	size, err := optionalFormValue(r.MultipartForm.Value, "size")
	if err != nil {
		return Request{}, err
	}
	files := append([]*multipart.FileHeader{}, r.MultipartForm.File["image"]...)
	files = append(files, r.MultipartForm.File["image[]"]...)
	if len(files) == 0 {
		return Request{}, &RequestError{Status: http.StatusBadRequest, Message: "at least one image is required", Param: "image"}
	}
	if len(files) > maxImageCount {
		return Request{}, &RequestError{Status: http.StatusBadRequest, Message: "at most 10 images are allowed", Param: "image"}
	}

	images := make([]imageFile, 0, len(files))
	for _, fileHeader := range files {
		image, err := readImage(fileHeader)
		if err != nil {
			return Request{}, err
		}
		images = append(images, image)
	}

	return buildMultipartEditRequest(prompt, size, images)
}

func hasMediaType(r *http.Request, expected string) bool {
	mediaType, _, err := mime.ParseMediaType(r.Header.Get("Content-Type"))
	return err == nil && strings.EqualFold(mediaType, expected)
}

func readBody(body io.ReadCloser, maxBytes int64) ([]byte, error) {
	defer body.Close()
	data, err := io.ReadAll(io.LimitReader(body, maxBytes+1))
	if err != nil {
		return nil, &RequestError{Status: http.StatusBadRequest, Message: "unable to read request body"}
	}
	if int64(len(data)) > maxBytes {
		return nil, &RequestError{Status: http.StatusRequestEntityTooLarge, Message: "request body is too large"}
	}
	return data, nil
}

func validatePrompt(value string) (string, error) {
	prompt := strings.TrimSpace(value)
	if prompt == "" {
		return "", &RequestError{Status: http.StatusBadRequest, Message: "prompt is required", Param: "prompt"}
	}
	if len(prompt) > maxPromptBytes {
		return "", &RequestError{Status: http.StatusRequestEntityTooLarge, Message: "prompt is too large", Param: "prompt"}
	}
	return prompt, nil
}

func requiredFormValue(values map[string][]string, name string) (string, error) {
	value, err := optionalFormValue(values, name)
	if err != nil {
		return "", err
	}
	if value == "" {
		return "", &RequestError{Status: http.StatusBadRequest, Message: name + " is required", Param: name}
	}
	return value, nil
}

func optionalFormValue(values map[string][]string, name string) (string, error) {
	valuesForName := values[name]
	if len(valuesForName) == 0 {
		return "", nil
	}
	if len(valuesForName) != 1 {
		return "", &RequestError{Status: http.StatusBadRequest, Message: name + " must be provided once", Param: name}
	}
	return valuesForName[0], nil
}

type imageFile struct {
	data        []byte
	contentType string
	extension   string
}

func readImage(header *multipart.FileHeader) (imageFile, error) {
	file, err := header.Open()
	if err != nil {
		return imageFile{}, &RequestError{Status: http.StatusBadRequest, Message: "unable to read image", Param: "image"}
	}
	defer file.Close()

	data, err := io.ReadAll(io.LimitReader(file, maxImageBytes+1))
	if err != nil {
		return imageFile{}, &RequestError{Status: http.StatusBadRequest, Message: "unable to read image", Param: "image"}
	}
	if int64(len(data)) > maxImageBytes {
		return imageFile{}, &RequestError{Status: http.StatusRequestEntityTooLarge, Message: "image is too large", Param: "image"}
	}

	contentType := http.DetectContentType(data)
	extension := ""
	switch contentType {
	case "image/png":
		extension = ".png"
	case "image/jpeg":
		extension = ".jpg"
	case "image/webp":
		extension = ".webp"
	default:
		return imageFile{}, &RequestError{Status: http.StatusBadRequest, Message: "image must be PNG, JPEG, or WebP", Param: "image"}
	}

	return imageFile{data: data, contentType: contentType, extension: extension}, nil
}

func buildMultipartEditRequest(prompt, size string, images []imageFile) (Request, error) {
	var body bytes.Buffer
	writer := multipart.NewWriter(&body)

	for _, field := range []struct {
		name  string
		value string
	}{
		{name: "model", value: imageModel},
		{name: "prompt", value: prompt},
		{name: "response_format", value: responseFormat},
	} {
		if err := writer.WriteField(field.name, field.value); err != nil {
			return Request{}, fmt.Errorf("write multipart %s: %w", field.name, err)
		}
	}
	if size != "" {
		if err := writer.WriteField("size", size); err != nil {
			return Request{}, fmt.Errorf("write multipart size: %w", err)
		}
	}

	for index, image := range images {
		header := make(textproto.MIMEHeader)
		header.Set("Content-Disposition", fmt.Sprintf(`form-data; name="image"; filename="image-%d%s"`, index+1, image.extension))
		header.Set("Content-Type", image.contentType)
		part, err := writer.CreatePart(header)
		if err != nil {
			return Request{}, fmt.Errorf("create image part: %w", err)
		}
		if _, err := part.Write(image.data); err != nil {
			return Request{}, fmt.Errorf("write image part: %w", err)
		}
	}

	if err := writer.Close(); err != nil {
		return Request{}, fmt.Errorf("close multipart request: %w", err)
	}

	return Request{
		Method:      http.MethodPost,
		Path:        EditsPath,
		ContentType: writer.FormDataContentType(),
		Body:        body.Bytes(),
	}, nil
}
