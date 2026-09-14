// Package account 负责 config.toml 的读写。
// 账号增删改查时需原子写回 config.toml，保留非 auth 段全部字段与注释，
// 仅重写 [[auth]] 段（用 go-toml/v2 Unmarshal + 结构体重组）。
package account

import (
	"fmt"
	"os"
	"path/filepath"
	"sync"

	"github.com/pelletier/go-toml/v2"

	"clodesen2api/internal/config"
)

// ConfigStore 管理 config.toml 的读写。
// 账号列表与调度策略持久化到 config.toml，写回时仅重写对应段，其余字段原样保留。
type ConfigStore struct {
	path string
	mu   sync.RWMutex
}

// NewConfigStore 创建 config.toml 存储。
func NewConfigStore(path string) *ConfigStore {
	return &ConfigStore{path: path}
}

// LoadAccounts 读取 config.toml 中的 [[auth]] 账号数组。
// 委托 config.LoadAccounts 实现，兼容旧式单值 [auth]。
func (s *ConfigStore) LoadAccounts() ([]config.AuthAccount, error) {
	s.mu.RLock()
	defer s.mu.RUnlock()
	return config.LoadAccounts(s.path)
}

// SaveAccounts 把账号数组原子写回 config.toml。
// 读取现有 config.toml 全文解析为通用 map，仅替换 auth 段为新的 [[auth]] 数组，
// 其余字段（server/upstream/checkin/account/schedule 等）原样保留。
// 注意：go-toml/v2 Marshal 会丢失注释，这是可接受的。
func (s *ConfigStore) SaveAccounts(accounts []config.AuthAccount) error {
	s.mu.Lock()
	defer s.mu.Unlock()

	// 读取现有配置全文
	data, err := os.ReadFile(s.path)
	if err != nil {
		return fmt.Errorf("account: read config file: %w", err)
	}

	// 解析为通用 map，保留非 auth 段全部字段
	var raw map[string]any
	if err := toml.Unmarshal(data, &raw); err != nil {
		return fmt.Errorf("account: decode config file: %w", err)
	}

	// 仅替换 auth 段为新的 [[auth]] 数组
	auth := make([]any, 0, len(accounts))
	for _, a := range accounts {
		auth = append(auth, map[string]any{
			"identifier":   a.Identifier,
			"password":     a.Password,
			"auto_relogin": a.AutoRelogin,
		})
	}
	raw["auth"] = auth

	// 序列化并原子写回
	return s.write(raw)
}

// LoadSchedule 读取 config.toml 中的 [schedule] 段。
// 委托 config.LoadSchedule 实现，未配置或 mode 为空时默认 mode="random"。
func (s *ConfigStore) LoadSchedule() (config.ScheduleConfig, error) {
	s.mu.RLock()
	defer s.mu.RUnlock()
	return config.LoadSchedule(s.path)
}

// SaveSchedule 把调度策略原子写回 config.toml。
// 读取现有 config.toml，仅更新 [schedule] 段（mode、fixed_id），其余字段原样保留。
func (s *ConfigStore) SaveSchedule(schedule config.ScheduleConfig) error {
	s.mu.Lock()
	defer s.mu.Unlock()

	data, err := os.ReadFile(s.path)
	if err != nil {
		return fmt.Errorf("account: read config file: %w", err)
	}

	var raw map[string]any
	if err := toml.Unmarshal(data, &raw); err != nil {
		return fmt.Errorf("account: decode config file: %w", err)
	}

	// 更新 [schedule] 段，空值删除对应字段（LoadSchedule 会回退到默认值）
	sec, ok := raw["schedule"].(map[string]any)
	if !ok {
		sec = make(map[string]any)
	}
	if schedule.Mode != "" {
		sec["mode"] = schedule.Mode
	} else {
		delete(sec, "mode")
	}
	if schedule.FixedID != "" {
		sec["fixed_id"] = schedule.FixedID
	} else {
		delete(sec, "fixed_id")
	}
	raw["schedule"] = sec

	return s.write(raw)
}

// write 把通用 map 序列化为 TOML 并原子写回 config.toml（0600 权限）。
// 先写临时文件再 rename，避免写一半导致文件损坏。
// 调用方需持有写锁。
func (s *ConfigStore) write(raw map[string]any) error {
	data, err := toml.Marshal(raw)
	if err != nil {
		return fmt.Errorf("account: marshal config: %w", err)
	}

	dir := filepath.Dir(s.path)
	tmp, err := os.CreateTemp(dir, "config-*.tmp")
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
