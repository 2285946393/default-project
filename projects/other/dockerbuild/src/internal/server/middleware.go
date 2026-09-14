// Package server 提供本地 HTTP 服务和路由。
package server

import (
	"encoding/json"
	"net/http"
	"strings"

	"clodesen2api/internal/account"
)

// sessionCookieName 定义会话 Cookie 的名称。
// 网页登录成功后由 /api/admin/login 写入，/api/admin/* 鉴权时读取。
const sessionCookieName = "session"

// writeAuthError 以 JSON 形式输出鉴权失败响应（401）。
// 与 handler.go 中 502 响应的写法保持一致：{"error":"..."}。
func writeAuthError(w http.ResponseWriter, message string) {
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(http.StatusUnauthorized)
	_ = json.NewEncoder(w).Encode(map[string]string{
		"error": message,
	})
}

// SessionMiddleware 会话鉴权中间件。
// 从请求 Cookie 中读取会话 token（Cookie 名 "session"），
// 调用 PasswordStore.ValidateSession 校验；未通过返回 401，通过则放行。
// 用于保护 /api/admin/* 等需要网页登录的接口。
func SessionMiddleware(pw *account.PasswordStore, next http.Handler) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		cookie, err := r.Cookie(sessionCookieName)
		if err != nil || cookie.Value == "" {
			writeAuthError(w, "unauthorized")
			return
		}
		if !pw.ValidateSession(cookie.Value) {
			writeAuthError(w, "unauthorized")
			return
		}
		next.ServeHTTP(w, r)
	})
}

// KeyAuthMiddleware Key 鉴权中间件。
// 从请求头 X-API-Key 或 Authorization: Bearer {key} 中提取 key，
// 调用 KeyStore.Validate 校验；失败返回 401，通过则放行。
// 用于保护网关 /v1/* 接口；key 头不传给上游，由 BuildRequest 丢弃客户端请求头实现。
func KeyAuthMiddleware(ks *account.KeyStore, next http.Handler) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		key := extractAPIKey(r)
		if key == "" || !ks.Validate(key) {
			writeAuthError(w, "invalid api key")
			return
		}
		next.ServeHTTP(w, r)
	})
}

// extractAPIKey 从请求头中提取 API Key。
// 优先取 X-API-Key；否则解析 Authorization: Bearer {key}。
// 两者都缺失或格式不合法时返回空字符串。
func extractAPIKey(r *http.Request) string {
	if key := r.Header.Get("X-API-Key"); key != "" {
		return key
	}

	auth := r.Header.Get("Authorization")
	const bearerPrefix = "Bearer "
	if len(auth) > len(bearerPrefix) && strings.EqualFold(auth[:len(bearerPrefix)], bearerPrefix) {
		return strings.TrimSpace(auth[len(bearerPrefix):])
	}
	return ""
}
