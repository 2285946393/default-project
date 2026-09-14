// Package server 提供本地 HTTP 服务和路由。
package server

import (
	"context"
	"encoding/json"
	"errors"
	"log"
	"net/http"
	"os"
	"time"

	"clodesen2api/internal/account"
	"clodesen2api/internal/openai"
)

var forwardedPaths = [...]string{
	openai.ModelsPath,
	openai.ChatCompletionsPath,
	openai.EditsPath,
	openai.GenerationsPath,
}

// Handler 持有网关转发所需的依赖。
// mgr 负责多账号调度选号，ks 负责 /v1/* 的 Key 鉴权。
// indexHTML 缓存根路径管理页面内容（NewHandler 时从 web/index.html 读取，失败回退占位 HTML）。
type Handler struct {
	mgr       *account.AccountManager
	ks        *account.KeyStore
	indexHTML []byte
}

// NewHandler 创建 HTTP 路由。
// 注册 / 静态页、/api/admin/* 管理 API 与 /v1/* 转发路由；
// /v1/* 先经 KeyAuthMiddleware 鉴权，再通过 mgr.Pick() 选择账号转发。
// 只有 forwardedPaths 中的精确路径会调用上游，其他路径由 ServeMux 返回 404。
func NewHandler(mgr *account.AccountManager, ks *account.KeyStore, admin *AdminHandler) http.Handler {
	h := &Handler{mgr: mgr, ks: ks, indexHTML: loadIndexHTML()}
	mux := http.NewServeMux()

	// 健康检查仅由本地处理，不会转发到上游。
	mux.HandleFunc("/healthz", func(w http.ResponseWriter, r *http.Request) {
		_, _ = w.Write([]byte("ok"))
	})

	// 根路径返回管理页面（占位 HTML，子任务12创建 web/index.html 后接入）。
	// 使用 /{$} 精确匹配根路径，避免吞掉 /healthz、/api/admin/*、/v1/* 等路由。
	mux.HandleFunc("/{$}", h.handleIndex)

	// 管理 API 路由（内部已按需套用会话鉴权中间件）。
	admin.RegisterRoutes(mux)

	// /v1/* 转发路由：先 Key 鉴权，再调度选号转发。
	for _, path := range forwardedPaths {
		mux.Handle(path, KeyAuthMiddleware(ks, http.HandlerFunc(h.forward)))
	}

	return mux
}

// forward 处理 /v1/* 转发：先经 KeyAuthMiddleware 鉴权（路由注册时包装），
// 再通过调度器选择账号，用该账号的 Upstream 转发。
func (h *Handler) forward(w http.ResponseWriter, r *http.Request) {
	request, err := openai.BuildRequest(w, r)
	if err != nil {
		writeRequestError(w, err)
		log.Printf("[request] rejected %s %s: %v", r.Method, r.URL.Path, err)
		return
	}

	// 通过调度器选择账号
	acc, err := h.mgr.Pick()
	if err != nil {
		log.Printf("[forward] pick account %s %s: %v", r.Method, r.URL.Path, err)
		w.Header().Set("Content-Type", "application/json")
		w.WriteHeader(http.StatusBadGateway)
		_ = json.NewEncoder(w).Encode(map[string]string{
			"error": err.Error(),
		})
		return
	}

	start := time.Now()
	err, wrote := acc.Upstream.Forward(r.Context(), w, request)
	cost := time.Since(start)
	if err != nil {
		// 尽量不覆盖已经写出的上游响应
		if errors.Is(err, context.Canceled) {
			log.Printf("[forward] client canceled %s %s cost=%s", r.Method, r.URL.Path, cost)
			return
		}
		log.Printf("[forward] error %s %s cost=%s: %v", r.Method, r.URL.Path, cost, err)
		// 仅当响应还没写时返回 502
		if !wrote {
			w.Header().Set("Content-Type", "application/json")
			w.WriteHeader(http.StatusBadGateway)
			_ = json.NewEncoder(w).Encode(map[string]string{
				"error": err.Error(),
			})
		}
		return
	}
	log.Printf("[forward] %s %s cost=%s", r.Method, r.URL.Path, cost)
}

// loadIndexHTML 读取 web/index.html 作为根路径管理页面内容。
// 使用相对路径读取（与可执行文件同目录部署，start.sh 从项目根目录启动），
// 读取失败时回退到内置占位 HTML，保证测试环境（无 web 目录）也能编译运行。
func loadIndexHTML() []byte {
	content, err := os.ReadFile("web/index.html")
	if err != nil {
		log.Printf("[server] 读取 web/index.html 失败，使用占位页面: %v", err)
		return []byte(indexHTML)
	}
	return content
}

// handleIndex 返回管理页面。
// 内容在 NewHandler 时从 web/index.html 读取并缓存；读取失败时回退到占位 HTML。
// 加 Cache-Control: no-cache 避免浏览器缓存旧页面（更新前端后无需手动强刷）。
func (h *Handler) handleIndex(w http.ResponseWriter, r *http.Request) {
	w.Header().Set("Content-Type", "text/html; charset=utf-8")
	w.Header().Set("Cache-Control", "no-cache, no-store, must-revalidate")
	_, _ = w.Write(h.indexHTML)
}

// indexHTML 是管理页面的占位内容，仅在 web/index.html 读取失败时作为回退。
const indexHTML = `<!DOCTYPE html>
<html lang="zh-CN">
<head>
	<meta charset="UTF-8">
	<title>clodesen2api 管理后台</title>
</head>
<body>
	<h1>clodesen2api 管理后台</h1>
	<p>管理页面待接入（web/index.html 由子任务12创建）。</p>
</body>
</html>`

func writeRequestError(w http.ResponseWriter, err error) {
	status := http.StatusBadRequest
	message := "invalid request"
	param := ""
	if requestErr, ok := openai.AsRequestError(err); ok {
		status = requestErr.Status
		message = requestErr.Message
		param = requestErr.Param
	}

	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(status)
	_ = json.NewEncoder(w).Encode(struct {
		Error struct {
			Message string `json:"message"`
			Type    string `json:"type"`
			Param   string `json:"param,omitempty"`
		} `json:"error"`
	}{
		Error: struct {
			Message string `json:"message"`
			Type    string `json:"type"`
			Param   string `json:"param,omitempty"`
		}{
			Message: message,
			Type:    "invalid_request_error",
			Param:   param,
		},
	})
}
