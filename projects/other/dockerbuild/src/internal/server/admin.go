// Package server 提供本地 HTTP 服务和路由。
package server

import (
	"encoding/json"
	"log"
	"net/http"
	"strings"
	"time"

	"clodesen2api/internal/account"
)

// AdminHandler 提供网页管理 API。
// 持有账号管理器、密码存储与 Key 存储，除 login/logout 外均需会话 Cookie 鉴权。
type AdminHandler struct {
	mgr *account.AccountManager
	pw  *account.PasswordStore
	ks  *account.KeyStore
}

// NewAdminHandler 创建管理 API 处理器。
func NewAdminHandler(mgr *account.AccountManager, pw *account.PasswordStore, ks *account.KeyStore) *AdminHandler {
	return &AdminHandler{
		mgr: mgr,
		pw:  pw,
		ks:  ks,
	}
}

// RegisterRoutes 注册全部管理 API 路由。
// /api/admin/login 与 /api/admin/logout 无需会话鉴权（login 是登录入口，
// logout 应允许未登录时调用）；其余 /api/admin/* 路由用 SessionMiddleware 包装。
func (h *AdminHandler) RegisterRoutes(mux *http.ServeMux) {
	// 无需鉴权的路由
	mux.HandleFunc("/api/admin/login", h.handleLogin)
	mux.HandleFunc("/api/admin/logout", h.handleLogout)

	// 需要会话鉴权的路由
	auth := func(pattern string, handler http.HandlerFunc) {
		mux.Handle(pattern, SessionMiddleware(h.pw, handler))
	}

	auth("/api/admin/change-password", h.handleChangePassword)
	auth("/api/admin/accounts", h.handleAccounts)
	auth("/api/admin/accounts/", h.handleAccountByID)
	auth("/api/admin/key", h.handleKey)
	auth("/api/admin/key/generate", h.handleKeyGenerate)
	auth("/api/admin/schedule", h.handleSchedule)
}

// handleLogin 处理网页登录：校验密码，成功则签发会话 Cookie。
func (h *AdminHandler) handleLogin(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodPost {
		writeMethodNotAllowed(w)
		return
	}

	var body struct {
		Password string `json:"password"`
	}
	if err := json.NewDecoder(r.Body).Decode(&body); err != nil {
		writeAdminError(w, http.StatusBadRequest, "invalid request body")
		return
	}

	if !h.pw.Verify(body.Password) {
		log.Printf("[admin] login failed")
		writeAdminError(w, http.StatusUnauthorized, "invalid password")
		return
	}

	token := h.pw.CreateSession()
	http.SetCookie(w, &http.Cookie{
		Name:     sessionCookieName,
		Value:    token,
		Path:     "/",
		HttpOnly: true,
	})
	log.Printf("[admin] login success")
	writeJSON(w, http.StatusOK, map[string]bool{"ok": true})
}

// handleLogout 处理退出登录：销毁会话并清除 Cookie。
func (h *AdminHandler) handleLogout(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodPost {
		writeMethodNotAllowed(w)
		return
	}

	if cookie, err := r.Cookie(sessionCookieName); err == nil && cookie.Value != "" {
		h.pw.DestroySession(cookie.Value)
	}
	http.SetCookie(w, &http.Cookie{
		Name:     sessionCookieName,
		Value:    "",
		Path:     "/",
		HttpOnly: true,
		MaxAge:   -1,
	})
	writeJSON(w, http.StatusOK, map[string]bool{"ok": true})
}

// handleChangePassword 处理修改密码：校验旧密码后写回 password.json。
func (h *AdminHandler) handleChangePassword(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodPost {
		writeMethodNotAllowed(w)
		return
	}

	var body struct {
		Old string `json:"old"`
		New string `json:"new"`
	}
	if err := json.NewDecoder(r.Body).Decode(&body); err != nil {
		writeAdminError(w, http.StatusBadRequest, "invalid request body")
		return
	}

	if err := h.pw.ChangePassword(body.Old, body.New); err != nil {
		writeAdminError(w, http.StatusBadRequest, err.Error())
		return
	}
	log.Printf("[admin] password changed")
	writeJSON(w, http.StatusOK, map[string]bool{"ok": true})
}

// handleAccounts 处理账号列表（GET）与新增账号（POST）。
func (h *AdminHandler) handleAccounts(w http.ResponseWriter, r *http.Request) {
	switch r.Method {
	case http.MethodGet:
		accounts := h.mgr.List()
		list := make([]accountView, 0, len(accounts))
		for _, a := range accounts {
			list = append(list, toAccountView(a))
		}
		writeJSON(w, http.StatusOK, map[string]any{"accounts": list})
	case http.MethodPost:
		var body struct {
			Identifier string `json:"identifier"`
			Password   string `json:"password"`
		}
		if err := json.NewDecoder(r.Body).Decode(&body); err != nil {
			writeAdminError(w, http.StatusBadRequest, "invalid request body")
			return
		}
		acc, err := h.mgr.Add(body.Identifier, body.Password, true)
		if err != nil {
			writeAdminError(w, http.StatusBadRequest, err.Error())
			return
		}
		log.Printf("[admin] account added: %s", acc.Identifier)
		writeJSON(w, http.StatusOK, toAccountView(acc))
	default:
		writeMethodNotAllowed(w)
	}
}

// handleAccountByID 处理单个账号的子路由：
// DELETE /api/admin/accounts/{id} 删除账号；
// POST /api/admin/accounts/{id}/relogin|check|balance|checkin 对应各操作。
func (h *AdminHandler) handleAccountByID(w http.ResponseWriter, r *http.Request) {
	// 路径形如 /api/admin/accounts/{id} 或 /api/admin/accounts/{id}/{action}
	rest := strings.TrimPrefix(r.URL.Path, "/api/admin/accounts/")
	parts := strings.Split(rest, "/")
	id := parts[0]
	action := ""
	if len(parts) > 1 {
		action = parts[1]
	}

	switch action {
	case "":
		// DELETE /api/admin/accounts/{id} 删除账号
		if r.Method != http.MethodDelete {
			writeMethodNotAllowed(w)
			return
		}
		if err := h.mgr.Delete(id); err != nil {
			writeAdminError(w, http.StatusBadRequest, err.Error())
			return
		}
		log.Printf("[admin] account deleted: %s", id)
		writeJSON(w, http.StatusOK, map[string]bool{"ok": true})
	case "relogin":
		// POST /api/admin/accounts/{id}/relogin 重新登录
		if r.Method != http.MethodPost {
			writeMethodNotAllowed(w)
			return
		}
		if err := h.mgr.ReLogin(id); err != nil {
			writeAdminError(w, http.StatusBadRequest, err.Error())
			return
		}
		writeJSON(w, http.StatusOK, map[string]bool{"ok": true})
	case "check":
		// POST /api/admin/accounts/{id}/check 检测（登录+余额+签到状态）
		if r.Method != http.MethodPost {
			writeMethodNotAllowed(w)
			return
		}
		acc, err := h.mgr.Check(id)
		if err != nil {
			if acc == nil {
				writeAdminError(w, http.StatusNotFound, err.Error())
				return
			}
			// 检测失败但账号存在，返回部分状态（含错误信息）
			writeJSON(w, http.StatusOK, toAccountView(acc))
			return
		}
		writeJSON(w, http.StatusOK, toAccountView(acc))
	case "balance":
		// POST /api/admin/accounts/{id}/balance 查询余额
		if r.Method != http.MethodPost {
			writeMethodNotAllowed(w)
			return
		}
		acc, err := h.mgr.Check(id)
		if err != nil {
			if acc == nil {
				writeAdminError(w, http.StatusNotFound, err.Error())
				return
			}
			// 账号存在但上游查询失败：返回 502 并带上错误原因，
			// 否则前端会因为 HTTP 200 而误报“查询成功”，掩盖真实故障。
			writeJSON(w, http.StatusBadGateway, map[string]any{
				"error":             err.Error(),
				"total_balance":     acc.TotalBalance,
				"temp_balance":      acc.TempBalance,
				"permanent_balance": acc.PermanentBalance,
				"temp_expire":       acc.TempExpire,
			})
			return
		}
		writeJSON(w, http.StatusOK, map[string]any{
			"total_balance":     acc.TotalBalance,
			"temp_balance":      acc.TempBalance,
			"permanent_balance": acc.PermanentBalance,
			"temp_expire":       acc.TempExpire,
		})
	case "checkin":
		// POST /api/admin/accounts/{id}/checkin 触发签到。
		// 直接调用 mgr.Checkin 让签到请求真正到达上游并更新缓存，
		// 不依据本地缓存 CheckedIn 提前跳过（幂等由上游处理）。
		if r.Method != http.MethodPost {
			writeMethodNotAllowed(w)
			return
		}
		if err := h.mgr.Checkin(id); err != nil {
			writeAdminError(w, http.StatusBadGateway, err.Error())
			return
		}
		acc, err := h.mgr.Get(id)
		if err != nil {
			writeAdminError(w, http.StatusNotFound, err.Error())
			return
		}
		log.Printf("[admin] checkin triggered: %s", id)
		writeJSON(w, http.StatusOK, map[string]any{
			"ok":           true,
			"checked_in":   acc.CheckedIn,
			"checkin_date": acc.CheckinDate,
		})
	default:
		writeAdminError(w, http.StatusNotFound, "not found")
	}
}

// handleKey 处理 Key 获取（GET）与自定义设置（POST）。
func (h *AdminHandler) handleKey(w http.ResponseWriter, r *http.Request) {
	switch r.Method {
	case http.MethodGet:
		writeJSON(w, http.StatusOK, map[string]string{"key": h.ks.GetKey()})
	case http.MethodPost:
		var body struct {
			Key string `json:"key"`
		}
		if err := json.NewDecoder(r.Body).Decode(&body); err != nil {
			writeAdminError(w, http.StatusBadRequest, "invalid request body")
			return
		}
		if err := h.ks.SetKey(body.Key); err != nil {
			writeAdminError(w, http.StatusBadRequest, err.Error())
			return
		}
		log.Printf("[admin] key updated")
		writeJSON(w, http.StatusOK, map[string]bool{"ok": true})
	default:
		writeMethodNotAllowed(w)
	}
}

// handleKeyGenerate 处理随机生成 Key（POST），写回并返回。
func (h *AdminHandler) handleKeyGenerate(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodPost {
		writeMethodNotAllowed(w)
		return
	}

	key, err := h.ks.GenerateKey()
	if err != nil {
		writeAdminError(w, http.StatusInternalServerError, err.Error())
		return
	}
	log.Printf("[admin] key regenerated")
	writeJSON(w, http.StatusOK, map[string]string{"key": key})
}

// handleSchedule 处理调度方式获取（GET）与设置（POST）。
func (h *AdminHandler) handleSchedule(w http.ResponseWriter, r *http.Request) {
	switch r.Method {
	case http.MethodGet:
		cfg := h.mgr.GetSchedule()
		writeJSON(w, http.StatusOK, map[string]string{
			"mode":     cfg.Mode,
			"fixed_id": cfg.FixedID,
		})
	case http.MethodPost:
		var body struct {
			Mode    string `json:"mode"`
			FixedID string `json:"fixed_id"`
		}
		if err := json.NewDecoder(r.Body).Decode(&body); err != nil {
			writeAdminError(w, http.StatusBadRequest, "invalid request body")
			return
		}
		if err := h.mgr.SetSchedule(body.Mode, body.FixedID); err != nil {
			writeAdminError(w, http.StatusBadRequest, err.Error())
			return
		}
		log.Printf("[admin] schedule set: mode=%s fixed_id=%s", body.Mode, body.FixedID)
		writeJSON(w, http.StatusOK, map[string]bool{"ok": true})
	default:
		writeMethodNotAllowed(w)
	}
}

// accountView 是账号的 JSON 视图，包含缓存状态字段（不暴露密码）。
type accountView struct {
	ID               string  `json:"id"`
	Identifier       string  `json:"identifier"`
	AutoRelogin      bool    `json:"auto_relogin"`
	Healthy          bool    `json:"healthy"`
	LastCheck        string  `json:"last_check,omitempty"`
	CheckedIn        bool    `json:"checked_in"`
	CheckinDate      string  `json:"checkin_date,omitempty"`
	TotalBalance     float64 `json:"total_balance"`
	TempBalance      float64 `json:"temp_balance"`
	PermanentBalance float64 `json:"permanent_balance"`
	TempExpire       string  `json:"temp_expire,omitempty"`
	Error            string  `json:"error,omitempty"`
}

// toAccountView 把内部 Account 转为 JSON 视图。
func toAccountView(a *account.Account) accountView {
	lastCheck := ""
	if !a.LastCheck.IsZero() {
		lastCheck = a.LastCheck.Format(time.RFC3339)
	}
	return accountView{
		ID:               a.ID,
		Identifier:       a.Identifier,
		AutoRelogin:      a.AutoRelogin,
		Healthy:          a.Healthy,
		LastCheck:        lastCheck,
		CheckedIn:        a.CheckedIn,
		CheckinDate:      a.CheckinDate,
		TotalBalance:     a.TotalBalance,
		TempBalance:      a.TempBalance,
		PermanentBalance: a.PermanentBalance,
		TempExpire:       a.TempExpire,
		Error:            a.Error,
	}
}

// writeJSON 以 JSON 形式输出响应。
func writeJSON(w http.ResponseWriter, status int, v any) {
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(status)
	_ = json.NewEncoder(w).Encode(v)
}

// writeAdminError 以 JSON 形式输出管理 API 错误响应。
func writeAdminError(w http.ResponseWriter, status int, message string) {
	writeJSON(w, status, map[string]string{"error": message})
}

// writeMethodNotAllowed 输出 405 方法不允许响应。
func writeMethodNotAllowed(w http.ResponseWriter) {
	writeAdminError(w, http.StatusMethodNotAllowed, "method not allowed")
}
