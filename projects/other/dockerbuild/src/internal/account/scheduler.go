// Package account 负责账号调度策略。
// 支持随机抽取、轮询、固定使用某账号、优先临时积分高四种调度方式，
// 调度方式由网页下拉框管理并持久化到 config.toml 的 [schedule] 段。
package account

import (
	"fmt"
	"math/rand"
	"sync"

	"clodesen2api/internal/config"
)

// 调度模式常量，对应 config.toml [schedule] 段的 mode 字段取值。
const (
	ModeRandom       = "random"        // 随机抽取健康账号
	ModeRoundRobin   = "round_robin"   // 轮询选择健康账号
	ModeFixed        = "fixed"         // 固定使用某账号
	ModePriorityTemp = "priority_temp" // 优先临时积分高的账号
)

// AccountInfo 表示参与调度的账号信息。
type AccountInfo struct {
	ID          string  // 账号唯一 ID
	Identifier  string  // 登录标识（邮箱/用户名）
	TempBalance float64 // 临时积分余额
	Healthy     bool    // 是否健康（可用）
}

// Scheduler 管理账号调度策略。
// 调度模式与固定账号 ID 存内存，由 ConfigStore 持久化到 config.toml。
type Scheduler struct {
	mu      sync.RWMutex
	mode    string // 当前调度模式
	fixedID string // 固定模式下的账号 ID
	rrIndex int    // 轮询模式下的当前索引
}

// NewScheduler 用初始配置创建调度器。
// mode 默认 random，fixedID 默认空。
func NewScheduler(initial config.ScheduleConfig) *Scheduler {
	mode := initial.Mode
	if mode == "" {
		mode = ModeRandom
	}
	return &Scheduler{
		mode:    mode,
		fixedID: initial.FixedID,
	}
}

// SetMode 校验并更新调度模式。
func (s *Scheduler) SetMode(mode string) error {
	switch mode {
	case ModeRandom, ModeRoundRobin, ModeFixed, ModePriorityTemp:
	default:
		return fmt.Errorf("account: 不支持的调度模式: %q", mode)
	}

	s.mu.Lock()
	defer s.mu.Unlock()
	s.mode = mode
	return nil
}

// SetFixed 设置固定模式下的账号 ID。
func (s *Scheduler) SetFixed(id string) error {
	if id == "" {
		return fmt.Errorf("account: 固定账号 ID 不能为空")
	}

	s.mu.Lock()
	defer s.mu.Unlock()
	s.fixedID = id
	return nil
}

// GetConfig 返回当前调度配置（mode 与 fixedID）。
func (s *Scheduler) GetConfig() config.ScheduleConfig {
	s.mu.RLock()
	defer s.mu.RUnlock()
	return config.ScheduleConfig{
		Mode:    s.mode,
		FixedID: s.fixedID,
	}
}

// Pick 根据当前调度模式从账号列表中选择一个账号 ID 返回。
// 除 priority_temp 外，健康池为空时保留其它策略的兼容性回退；余额优先策略
// 必须使用已检测的健康账号，避免未初始化的零余额参与排序。
func (s *Scheduler) Pick(accounts []AccountInfo) (string, error) {
	if len(accounts) == 0 {
		return "", fmt.Errorf("account: 账号列表为空，无法调度")
	}

	pool := make([]AccountInfo, 0, len(accounts))
	for _, a := range accounts {
		if a.Healthy {
			pool = append(pool, a)
		}
	}

	s.mu.RLock()
	mode := s.mode
	fixedID := s.fixedID
	s.mu.RUnlock()
	if len(pool) == 0 {
		if mode == ModePriorityTemp {
			return "", fmt.Errorf("account: 没有健康账号可供调度")
		}
		pool = accounts
	}

	switch mode {
	case ModeRoundRobin:
		return s.pickRoundRobin(pool)
	case ModeFixed:
		return s.pickFixed(pool, fixedID)
	case ModePriorityTemp:
		return s.pickPriorityTemp(pool)
	default: // ModeRandom 及未知模式均回退随机
		return s.pickRandom(pool)
	}
}

// pickRandom 从账号池中随机选一个。
func (s *Scheduler) pickRandom(pool []AccountInfo) (string, error) {
	return pool[rand.Intn(len(pool))].ID, nil
}

// pickRoundRobin 轮询选择账号，rrIndex 递增取模。
func (s *Scheduler) pickRoundRobin(pool []AccountInfo) (string, error) {
	s.mu.Lock()
	defer s.mu.Unlock()

	idx := s.rrIndex % len(pool)
	s.rrIndex = (s.rrIndex + 1) % len(pool)
	return pool[idx].ID, nil
}

// pickFixed 返回 fixedID 指定的账号；固定账号不可用时回退随机。
func (s *Scheduler) pickFixed(pool []AccountInfo, fixedID string) (string, error) {
	if fixedID != "" {
		for _, a := range pool {
			if a.ID == fixedID {
				return a.ID, nil
			}
		}
	}
	return s.pickRandom(pool)
}

// pickPriorityTemp 从账号池中选临时余额最高的账号。
func (s *Scheduler) pickPriorityTemp(pool []AccountInfo) (string, error) {
	best := pool[0]
	for _, a := range pool[1:] {
		if a.TempBalance > best.TempBalance {
			best = a
		}
	}
	return best.ID, nil
}
