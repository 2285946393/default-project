// Package account 负责网页登录的密码管理与会话 token 管理。
// 密码持久化到 password.json（与 config.toml 同目录），会话 token 仅存内存。
package account

import (
	"crypto/rand"
	"encoding/hex"
	"encoding/json"
	"errors"
	"fmt"
	"os"
	"path/filepath"
	"sync"
	"time"
)

// 默认密码：首次创建 password.json 时写入。
const defaultPassword = "qq971971"

// 会话有效期：24 小时。
const sessionTTL = 24 * time.Hour

// passwordFile 定义 password.json 的磁盘格式。
type passwordFile struct {
	Password string `json:"password"`
}

// PasswordStore 管理网页登录密码与会话 token。
// 密码持久化到磁盘，会话 token 仅存内存（进程重启后失效）。
type PasswordStore struct {
	path     string
	mu       sync.RWMutex
	password string
	sessions map[string]time.Time // token -> 过期时间
}

// NewPasswordStore 创建密码存储。
// 若 password.json 不存在则创建并写入默认密码 qq971971；存在则读取。
func NewPasswordStore(path string) (*PasswordStore, error) {
	s := &PasswordStore{
		path:     path,
		sessions: make(map[string]time.Time),
	}

	data, err := os.ReadFile(path)
	if err != nil {
		if !errors.Is(err, os.ErrNotExist) {
			return nil, fmt.Errorf("account: read password file: %w", err)
		}
		// 文件不存在，写入默认密码
		s.password = defaultPassword
		if err := s.save(); err != nil {
			return nil, err
		}
		return s, nil
	}

	var pf passwordFile
	if err := json.Unmarshal(data, &pf); err != nil {
		return nil, fmt.Errorf("account: decode password file: %w", err)
	}
	s.password = pf.Password
	return s, nil
}

// Verify 校验密码是否正确。
func (s *PasswordStore) Verify(password string) bool {
	s.mu.RLock()
	defer s.mu.RUnlock()
	return s.password == password
}

// ChangePassword 校验旧密码后更新为新密码，并原子写回 password.json。
// 先写临时文件再 rename，避免写一半导致文件损坏。
func (s *PasswordStore) ChangePassword(oldPwd, newPwd string) error {
	s.mu.Lock()
	defer s.mu.Unlock()

	if s.password != oldPwd {
		return errors.New("account: 旧密码不正确")
	}
	if newPwd == "" {
		return errors.New("account: 新密码不能为空")
	}

	s.password = newPwd
	return s.save()
}

// save 把当前密码原子写回 password.json（0600 权限）。
// 调用方需持有写锁。
func (s *PasswordStore) save() error {
	data, err := json.Marshal(passwordFile{Password: s.password})
	if err != nil {
		return fmt.Errorf("account: marshal password: %w", err)
	}

	dir := filepath.Dir(s.path)
	tmp, err := os.CreateTemp(dir, "password-*.tmp")
	if err != nil {
		return fmt.Errorf("account: create temp file: %w", err)
	}
	tmpName := tmp.Name()
	defer os.Remove(tmpName) // rename 成功后删除是空操作

	if err := tmp.Chmod(0o600); err != nil {
		_ = tmp.Close()
		return fmt.Errorf("account: chmod temp file: %w", err)
	}
	if _, err := tmp.Write(data); err != nil {
		_ = tmp.Close()
		return fmt.Errorf("account: write temp file: %w", err)
	}
	if err := tmp.Close(); err != nil {
		return fmt.Errorf("account: close temp file: %w", err)
	}
	if err := os.Rename(tmpName, s.path); err != nil {
		return fmt.Errorf("account: rename temp file: %w", err)
	}
	return nil
}

// CreateSession 生成随机会话 token（crypto/rand，32 字节 hex），
// 存入内存并设置 24 小时过期时间，返回 token。
func (s *PasswordStore) CreateSession() string {
	buf := make([]byte, 32)
	if _, err := rand.Read(buf); err != nil {
		// crypto/rand 失败几乎不可能，回退到时间戳+随机数兜底
		return fmt.Sprintf("%d-%d", time.Now().UnixNano(), os.Getpid())
	}
	token := hex.EncodeToString(buf)

	s.mu.Lock()
	s.sessions[token] = time.Now().Add(sessionTTL)
	s.mu.Unlock()
	return token
}

// ValidateSession 校验 token 是否存在且未过期，过期则删除。
func (s *PasswordStore) ValidateSession(token string) bool {
	s.mu.Lock()
	defer s.mu.Unlock()

	expire, ok := s.sessions[token]
	if !ok {
		return false
	}
	if time.Now().After(expire) {
		delete(s.sessions, token)
		return false
	}
	return true
}

// DestroySession 删除指定会话。
func (s *PasswordStore) DestroySession(token string) {
	s.mu.Lock()
	defer s.mu.Unlock()
	delete(s.sessions, token)
}

// CleanupExpired 清理所有已过期的会话，返回清理数量。
// 可定期调用（如每小时），简单实现即可。
func (s *PasswordStore) CleanupExpired() int {
	s.mu.Lock()
	defer s.mu.Unlock()

	now := time.Now()
	removed := 0
	for token, expire := range s.sessions {
		if now.After(expire) {
			delete(s.sessions, token)
			removed++
		}
	}
	return removed
}
