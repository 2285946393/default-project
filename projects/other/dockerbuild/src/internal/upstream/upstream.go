// Package upstream 负责管理 cookie、登录，并把请求转发到 pai.zaiduyu.top。
package upstream

import (
	"bytes"
	"context"
	"encoding/json"
	"errors"
	"fmt"
	"io"
	"net/http"
	"net/http/cookiejar"
	"strings"
	"sync"
	"time"

	"clodesen2api/internal/config"
	"clodesen2api/internal/httpx"
	"clodesen2api/internal/openai"
)

// Upstream 负责管理 cookie、登录、并把请求转发到 pai.zaiduyu.top。
type Upstream struct {
	cfg *config.Config

	httpClient *http.Client
	// loginClient 用独立短超时，避免登录卡住
	loginClient *http.Client

	mu          sync.Mutex
	loggedIn    bool
	lastRelogin time.Time
}

const browserUA = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36"

// maxJSONBody 是读取上游 JSON 响应体的上限（1MB）。
// 上游 /api/account 会带很长的 transactions 数组，旧的 4KB 上限会把响应体截断，
// 导致 json.Unmarshal 报 "unexpected end of JSON input"。这里放宽到 1MB，
// 既能容纳完整响应，又能防御异常超大响应。
const maxJSONBody = 1 << 20

// maxBodySnippet 是错误信息中回显响应体的最大字节数，避免把上万字符的流水刷进日志。
const maxBodySnippet = 512

// bodySnippet 返回用于错误信息的响应体片段，超长时截断并标注原始长度。
func bodySnippet(body []byte) string {
	if len(body) <= maxBodySnippet {
		return string(body)
	}
	return fmt.Sprintf("%s...(truncated, total=%d bytes)", body[:maxBodySnippet], len(body))
}

// New 构造上游客户端。cookiejar 用来自动保存 set-cookie。
func New(cfg *config.Config) (*Upstream, error) {
	jar, err := cookiejar.New(nil)
	if err != nil {
		return nil, fmt.Errorf("new cookie jar: %w", err)
	}

	transport := &http.Transport{
		// 流式响应不能设全局超时，这里只控制拨号/读首字节的超时
		ResponseHeaderTimeout: 60 * time.Second,
		IdleConnTimeout:       90 * time.Second,
		ForceAttemptHTTP2:     true,
	}

	mainTimeout := cfg.Upstream.RequestTimeout
	if mainTimeout > 0 {
		return &Upstream{
			cfg:         cfg,
			httpClient:  &http.Client{Transport: transport, Jar: jar, Timeout: mainTimeout},
			loginClient: &http.Client{Transport: transport, Jar: jar, Timeout: cfg.Upstream.LoginTimeout},
		}, nil
	}
	// request_timeout=0 表示不限超时（流式）
	return &Upstream{
		cfg:         cfg,
		httpClient:  &http.Client{Transport: transport, Jar: jar},
		loginClient: &http.Client{Transport: transport, Jar: jar, Timeout: cfg.Upstream.LoginTimeout},
	}, nil
}

// Location 返回配置的签到时区，供 Checkin 计算日期。
func (u *Upstream) Location() *time.Location {
	if loc, err := time.LoadLocation(u.cfg.Checkin.Timezone); err == nil {
		return loc
	}
	return time.Local
}

// OpenAIBaseURL 返回 OpenAI 兼容接口的基础地址。登录和签到仍使用 BaseURL。
func (u *Upstream) OpenAIBaseURL() string {
	if baseURL := strings.TrimSpace(u.cfg.Upstream.OpenAIBaseURL); baseURL != "" {
		return strings.TrimRight(baseURL, "/")
	}
	return strings.TrimRight(u.cfg.Upstream.BaseURL, "/") + "/api/openai"
}

// userAgent 取配置里的 UA，空则回退到内置浏览器 UA。
func (u *Upstream) userAgent() string {
	if ua := strings.TrimSpace(u.cfg.Upstream.UserAgent); ua != "" {
		return ua
	}
	return browserUA
}

// applyHeaders 给每个出站请求注入浏览器必备 header，确保不被上游 WAF 拒绝。
func (u *Upstream) applyHeaders(req *http.Request, host string) {
	req.Header.Set("User-Agent", u.userAgent())
	req.Header.Set("Accept", "application/json, text/event-stream, */*;q=0.8")
	req.Header.Set("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8")
	req.Header.Set("Origin", host)
	req.Header.Set("Referer", host+"/")
	// 让 Go 看起来像 XHR
	req.Header.Set("X-Requested-With", "XMLHttpRequest")
}

// Login 走 /api/user-auth 登录拿 cookie。成功后 cookiejar 会自动保存 set-cookie。
func (u *Upstream) Login(ctx context.Context) error {
	u.mu.Lock()
	defer u.mu.Unlock()

	// 简单防抖：1 秒内重复 401 不重复登录
	if time.Since(u.lastRelogin) < time.Second {
		if u.loggedIn {
			return nil
		}
	}

	host := strings.TrimRight(u.cfg.Upstream.BaseURL, "/")
	body := map[string]string{
		"action":     "login",
		"identifier": u.cfg.Auth.Identifier,
		"password":   u.cfg.Auth.Password,
	}
	raw, _ := json.Marshal(body)

	req, err := http.NewRequestWithContext(ctx, http.MethodPost, host+u.cfg.Upstream.LoginPath, bytes.NewReader(raw))
	if err != nil {
		return fmt.Errorf("build login request: %w", err)
	}
	req.Header.Set("Content-Type", "application/json")
	u.applyHeaders(req, host)

	resp, err := u.loginClient.Do(req)
	if err != nil {
		return fmt.Errorf("do login request: %w", err)
	}
	defer resp.Body.Close()

	// 读响应体用于错误信息
	respBody, _ := io.ReadAll(io.LimitReader(resp.Body, maxJSONBody))

	if resp.StatusCode/100 != 2 {
		u.loggedIn = false
		return fmt.Errorf("login failed: upstream status %d, body=%s", resp.StatusCode, bodySnippet(respBody))
	}

	// 校验响应体不是错误 JSON（有些上游 200 也返回 {"error":...}）
	var probe struct {
		Error string `json:"error"`
	}
	if err := json.Unmarshal(respBody, &probe); err == nil && probe.Error != "" {
		u.loggedIn = false
		return fmt.Errorf("login rejected by upstream: %s", probe.Error)
	}

	u.loggedIn = true
	u.lastRelogin = time.Now()
	return nil
}

// ensureLogin 首次调用或未登录时触发一次登录。
func (u *Upstream) ensureLogin(ctx context.Context) error {
	u.mu.Lock()
	logged := u.loggedIn
	u.mu.Unlock()
	if logged {
		return nil
	}
	return u.Login(ctx)
}

// Forward 把本地构造的 OpenAI 兼容请求转发到上游，返回上游的完整响应。
// 若上游回 401 且开启 auto-relogin，会重新登录后重试一次。
// 第二个返回值 wrote 表示响应头是否已经写给客户端（用于错误处理）。
func (u *Upstream) Forward(ctx context.Context, w http.ResponseWriter, request openai.Request) (error, bool) {
	host := strings.TrimRight(u.cfg.Upstream.BaseURL, "/")
	openAIBaseURL := u.OpenAIBaseURL()

	// 首次/未登录先登录
	if err := u.ensureLogin(ctx); err != nil {
		return fmt.Errorf("ensure login: %w", err), false
	}

	doForward := func() (*http.Response, error) {
		req, err := http.NewRequestWithContext(ctx, request.Method, openAIBaseURL+request.Path, bytes.NewReader(request.Body))
		if err != nil {
			return nil, err
		}
		if request.ContentType != "" {
			req.Header.Set("Content-Type", request.ContentType)
		}
		u.applyHeaders(req, host)
		return u.httpClient.Do(req)
	}

	resp, err := doForward()
	if err != nil {
		return fmt.Errorf("forward request: %w", err), false
	}

	// 401 自动重新登录后重试一次
	if resp.StatusCode == http.StatusUnauthorized && u.cfg.Auth.AutoRelogin {
		_ = resp.Body.Close()
		// 标记失效
		u.mu.Lock()
		u.loggedIn = false
		u.mu.Unlock()

		if err := u.Login(ctx); err != nil {
			return fmt.Errorf("relogin on 401: %w", err), false
		}
		// 再转一次
		resp, err = doForward()
		if err != nil {
			return fmt.Errorf("forward after relogin: %w", err), false
		}
	}

	// 把上游响应原样回写给客户端（含流式 SSE）
	httpx.CopyHeaders(w.Header(), resp.Header)
	w.WriteHeader(resp.StatusCode)
	writer := io.Writer(w)
	if strings.HasPrefix(strings.ToLower(resp.Header.Get("Content-Type")), "text/event-stream") {
		if flusher, ok := w.(http.Flusher); ok {
			writer = flushingWriter{writer: w, flusher: flusher}
		}
	}
	_, err = io.Copy(writer, resp.Body)
	_ = resp.Body.Close()
	if err != nil && !errors.Is(err, io.EOF) {
		// 客户端断开之类，不再处理
		return nil, true
	}
	return nil, true
}

type flushingWriter struct {
	writer  io.Writer
	flusher http.Flusher
}

func (w flushingWriter) Write(data []byte) (int, error) {
	n, err := w.writer.Write(data)
	w.flusher.Flush()
	return n, err
}

// CheckinResult 解析 /api/checkin 的响应字段，用于日志和幂等判断。
type CheckinResult struct {
	Enabled   *bool  `json:"enabled,omitempty"`
	CheckedIn bool   `json:"checkedIn"`
	Reward    int64  `json:"reward"`
	Date      string `json:"date"`
	Balance   int64  `json:"balance"`
	Success   *bool  `json:"success,omitempty"`
	Message   string `json:"message,omitempty"`
	Error     string `json:"error,omitempty"`
}

// Checkin 触发一次签到请求（GET）。未登录会先登录；遇到 401 会重登后重试一次。
// 返回解析后的签到结果。
func (u *Upstream) Checkin(ctx context.Context) (CheckinResult, error) {
	res, err := u.doCheckin(ctx, http.MethodGet, u.cfg.Checkin.Path, "checkin")
	if err != nil || res.CheckedIn {
		return res, err
	}
	// 部分上游把 GET /api/checkin 作为状态查询，真正执行签到要求 POST。
	// 先兼容原有 GET 接口；只有 GET 明确返回 checkedIn=false 时才尝试 POST。
	return u.doCheckin(ctx, http.MethodPost, u.cfg.Checkin.Path, "checkin (POST fallback)")
}

// HasCheckinStatusPath 表示是否配置了独立的签到状态查询端点。
func (u *Upstream) HasCheckinStatusPath() bool {
	return strings.TrimSpace(u.cfg.Checkin.StatusPath) != ""
}

// CheckinStatus 查询一次签到状态（GET）。未登录会先登录；遇到 401 会重登后重试一次。
// 使用 [checkin] status_path 配置的独立状态端点。
// 返回解析后的签到状态（checkedIn/date/balance）。
func (u *Upstream) CheckinStatus(ctx context.Context) (CheckinResult, error) {
	return u.doCheckin(ctx, http.MethodGet, strings.TrimSpace(u.cfg.Checkin.StatusPath), "checkin status")
}

// doCheckin 构造 GET 请求并解析签到/签到状态响应，供 Checkin 与 CheckinStatus 复用。
// 请求带 cookie、UA、X-Requested-With 等浏览器头；未登录会先登录；遇到 401 会重登后重试一次。
func (u *Upstream) doCheckin(ctx context.Context, method, path, label string) (CheckinResult, error) {
	host := strings.TrimRight(u.cfg.Upstream.BaseURL, "/")
	var res CheckinResult

	doCheckin := func() (*http.Response, error) {
		var body io.Reader
		if method == http.MethodPost {
			body = strings.NewReader(`{}`)
		}
		req, err := http.NewRequestWithContext(ctx, method, host+path, body)
		if err != nil {
			return nil, err
		}
		u.applyHeaders(req, host)
		if method == http.MethodPost {
			req.Header.Set("Content-Type", "application/json")
		}
		return u.loginClient.Do(req)
	}

	if err := u.ensureLogin(ctx); err != nil {
		return res, fmt.Errorf("ensure login: %w", err)
	}

	resp, err := doCheckin()
	if err != nil {
		return res, fmt.Errorf("%s request: %w", label, err)
	}
	body, _ := io.ReadAll(io.LimitReader(resp.Body, maxJSONBody))
	_ = resp.Body.Close()

	// 401 自动重登重试一次
	if resp.StatusCode == http.StatusUnauthorized && u.cfg.Auth.AutoRelogin {
		u.mu.Lock()
		u.loggedIn = false
		u.mu.Unlock()
		if err := u.Login(ctx); err != nil {
			return res, fmt.Errorf("relogin on 401: %w", err)
		}
		resp, err = doCheckin()
		if err != nil {
			return res, fmt.Errorf("%s after relogin: %w", label, err)
		}
		body, _ = io.ReadAll(io.LimitReader(resp.Body, maxJSONBody))
		_ = resp.Body.Close()
	}

	if resp.StatusCode/100 != 2 {
		return res, fmt.Errorf("%s failed: status=%d body=%s", label, resp.StatusCode, bodySnippet(body))
	}
	if err := json.Unmarshal(body, &res); err != nil {
		return res, fmt.Errorf("decode %s response: %w (body=%s)", label, err, bodySnippet(body))
	}
	// HTTP 2xx 只代表请求到达应用层；显式业务失败仍必须返回错误，
	// 否则管理页面会把“请求成功但签到失败”误显示为“签到成功”。
	if res.Error != "" {
		return res, fmt.Errorf("%s rejected by upstream: %s", label, res.Error)
	}
	if res.Success != nil && !*res.Success {
		message := res.Message
		if message == "" {
			message = "upstream returned success=false"
		}
		return res, fmt.Errorf("%s rejected by upstream: %s", label, message)
	}
	if res.Enabled != nil && !*res.Enabled {
		return res, fmt.Errorf("%s rejected by upstream: check-in is disabled", label)
	}
	return res, nil
}

// AccountResult 解析 /api/account 的响应字段，余额位于嵌套的 account 对象里。
// 只保留 4 个余额字段，不解析 user、transactions 等多余内容。
type AccountResult struct {
	Account struct {
		Balance          int64  `json:"balance"`
		LimitedBalance   int64  `json:"limitedBalance"`
		PermanentBalance int64  `json:"permanentBalance"`
		NextExpiryAt     string `json:"nextExpiryAt"`
	} `json:"account"`
}

// Account 查询一次账号余额（GET）。未登录会先登录；遇到 401 会重登后重试一次。
// 返回解析后的余额信息。
func (u *Upstream) Account(ctx context.Context) (AccountResult, error) {
	host := strings.TrimRight(u.cfg.Upstream.BaseURL, "/")
	var res AccountResult

	doAccount := func() (*http.Response, error) {
		req, err := http.NewRequestWithContext(ctx, http.MethodGet, host+u.cfg.Account.Path, nil)
		if err != nil {
			return nil, err
		}
		u.applyHeaders(req, host)
		return u.loginClient.Do(req)
	}

	if err := u.ensureLogin(ctx); err != nil {
		return res, fmt.Errorf("ensure login: %w", err)
	}

	resp, err := doAccount()
	if err != nil {
		return res, fmt.Errorf("account request: %w", err)
	}
	body, _ := io.ReadAll(io.LimitReader(resp.Body, maxJSONBody))
	_ = resp.Body.Close()

	// 401 自动重登重试一次
	if resp.StatusCode == http.StatusUnauthorized && u.cfg.Auth.AutoRelogin {
		u.mu.Lock()
		u.loggedIn = false
		u.mu.Unlock()
		if err := u.Login(ctx); err != nil {
			return res, fmt.Errorf("relogin on 401: %w", err)
		}
		resp, err = doAccount()
		if err != nil {
			return res, fmt.Errorf("account after relogin: %w", err)
		}
		body, _ = io.ReadAll(io.LimitReader(resp.Body, maxJSONBody))
		_ = resp.Body.Close()
	}

	if resp.StatusCode/100 != 2 {
		return res, fmt.Errorf("account query failed: status=%d body=%s", resp.StatusCode, bodySnippet(body))
	}
	// encoding/json 会自动忽略结构体中未声明的字段（user、transactions 等），无需裁剪响应体
	if err := json.Unmarshal(body, &res); err != nil {
		return res, fmt.Errorf("decode account response: %w (body=%s)", err, bodySnippet(body))
	}
	return res, nil
}
