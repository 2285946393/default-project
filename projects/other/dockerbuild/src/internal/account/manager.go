// Package account 负责多账号生命周期管理。
// AccountManager 持有全部账号及其独立上游客户端，提供增删改查、热更新、
// 重新登录、健康检测、签到与调度选择能力，并通过 sync.RWMutex 保证并发安全。
package account

import (
	"context"
	"fmt"
	"math/rand"
	"sync"
	"time"

	"clodesen2api/internal/config"
	"clodesen2api/internal/upstream"
)

// Account 表示一个上游登录账号及其运行时状态。
// 缓存字段（CheckedIn/CheckinDate/余额等）由 Check 检测时刷新，
// 读取时建议通过 AccountManager 的 List/Get 获取以保证并发安全。
type Account struct {
	ID          string // 账号唯一 ID
	Identifier  string // 登录标识（邮箱/用户名）
	Password    string // 登录密码
	AutoRelogin bool   // 401 时是否自动重新登录

	Upstream  *upstream.Upstream // 该账号独立的上游客户端
	Healthy   bool               // 是否健康（最近一次检测通过）
	LastCheck time.Time          // 最近一次检测时间

	// 状态缓存（由 Check 刷新）
	CheckedIn        bool    // 今天是否已签到
	CheckinDate      string  // 最近签到日期 YYYY-MM-DD
	TotalBalance     float64 // 总余额
	TempBalance      float64 // 临时积分余额
	PermanentBalance float64 // 永久积分余额
	TempExpire       string  // 临时积分到期时间
	Error            string  // 最近一次检测/操作错误信息
}

// AccountManager 管理多账号生命周期。
// 启动时从 ConfigStore 读取全部账号并为每个账号创建独立 *upstream.Upstream，
// 提供增删改查、热更新、重新登录、健康检测、签到与调度选择能力。
type AccountManager struct {
	mu        sync.RWMutex
	accounts  []*Account
	store     *ConfigStore
	scheduler *Scheduler
	baseCfg   *config.Config // 基础配置，用于为每个账号派生独立 Upstream
}

// NewAccountManager 创建多账号管理器。
// 启动时从 store 读取全部账号，为每个账号创建独立 *upstream.Upstream；
// baseCfg 提供 upstream/checkin/account 等公共配置，账号凭据取自 [[auth]] 段。
func NewAccountManager(store *ConfigStore, scheduler *Scheduler, baseCfg *config.Config) (*AccountManager, error) {
	m := &AccountManager{
		store:     store,
		scheduler: scheduler,
		baseCfg:   baseCfg,
	}

	authAccounts, err := store.LoadAccounts()
	if err != nil {
		return nil, fmt.Errorf("account: 读取账号列表失败: %w", err)
	}

	for _, a := range authAccounts {
		up, err := m.newUpstream(a.Identifier, a.Password, a.AutoRelogin)
		if err != nil {
			return nil, fmt.Errorf("account: 初始化账号 %q 的上游客户端失败: %w", a.Identifier, err)
		}
		m.accounts = append(m.accounts, &Account{
			ID:          genID(),
			Identifier:  a.Identifier,
			Password:    a.Password,
			AutoRelogin: a.AutoRelogin,
			Upstream:    up,
		})
	}
	return m, nil
}

// newUpstream 基于基础配置为指定账号创建独立上游客户端。
// config.Config 全部为值类型字段，浅拷贝后仅替换 Auth 段即可，不影响基础配置。
func (m *AccountManager) newUpstream(identifier, password string, autoRelogin bool) (*upstream.Upstream, error) {
	cfg := *m.baseCfg
	cfg.Auth.Identifier = identifier
	cfg.Auth.Password = password
	cfg.Auth.AutoRelogin = autoRelogin
	return upstream.New(&cfg)
}

// genID 生成账号唯一 ID：时间戳 + 随机数，保证唯一。
func genID() string {
	return fmt.Sprintf("%d%04d", time.Now().UnixNano(), rand.Intn(10000))
}

// List 返回全部账号（含缓存状态）。
func (m *AccountManager) List() []*Account {
	m.mu.RLock()
	defer m.mu.RUnlock()
	accounts := make([]*Account, len(m.accounts))
	copy(accounts, m.accounts)
	return accounts
}

// Get 按 ID 获取账号。
func (m *AccountManager) Get(id string) (*Account, error) {
	m.mu.RLock()
	defer m.mu.RUnlock()
	for _, a := range m.accounts {
		if a.ID == id {
			return a, nil
		}
	}
	return nil, fmt.Errorf("account: 账号 %q 不存在", id)
}

// Add 新增账号：创建独立 Upstream 并写回 config.toml。
// 写回失败时回滚内存中的账号列表，保证内存与磁盘一致。
func (m *AccountManager) Add(identifier, password string, autoRelogin bool) (*Account, error) {
	if identifier == "" || password == "" {
		return nil, fmt.Errorf("account: 账号标识与密码不能为空")
	}

	up, err := m.newUpstream(identifier, password, autoRelogin)
	if err != nil {
		return nil, fmt.Errorf("account: 初始化账号 %q 的上游客户端失败: %w", identifier, err)
	}

	acc := &Account{
		ID:          genID(),
		Identifier:  identifier,
		Password:    password,
		AutoRelogin: autoRelogin,
		Upstream:    up,
	}

	m.mu.Lock()
	m.accounts = append(m.accounts, acc)
	if err := m.saveAccountsLocked(); err != nil {
		// 写回失败则回滚，避免内存与磁盘不一致
		m.accounts = m.accounts[:len(m.accounts)-1]
		m.mu.Unlock()
		return nil, err
	}
	m.mu.Unlock()
	return acc, nil
}

// Delete 删除账号并写回 config.toml。
func (m *AccountManager) Delete(id string) error {
	m.mu.Lock()
	defer m.mu.Unlock()
	for i, a := range m.accounts {
		if a.ID == id {
			m.accounts = append(m.accounts[:i], m.accounts[i+1:]...)
			return m.saveAccountsLocked()
		}
	}
	return fmt.Errorf("account: 账号 %q 不存在", id)
}

// ReLogin 重新登录指定账号。
func (m *AccountManager) ReLogin(id string) error {
	acc, err := m.Get(id)
	if err != nil {
		return err
	}
	if err := acc.Upstream.Login(context.Background()); err != nil {
		m.setError(acc, err)
		return fmt.Errorf("account: 账号 %q 重新登录失败: %w", id, err)
	}
	m.setHealthy(acc, true)
	return nil
}

// Check 检测账号：登录 + 余额查询 + 签到状态查询，并刷新缓存状态。
// 任一环节失败都会记录错误并标记不健康，但返回的账号仍可用于查看部分状态。
func (m *AccountManager) Check(id string) (*Account, error) {
	acc, err := m.Get(id)
	if err != nil {
		return nil, err
	}

	ctx := context.Background()

	// 登录
	if err := acc.Upstream.Login(ctx); err != nil {
		m.setError(acc, err)
		return acc, fmt.Errorf("account: 账号 %q 登录失败: %w", id, err)
	}

	// 余额查询
	accRes, err := acc.Upstream.Account(ctx)
	if err != nil {
		m.setError(acc, err)
		return acc, fmt.Errorf("account: 账号 %q 余额查询失败: %w", id, err)
	}

	// 签到状态查询仅在显式配置 status_path 时执行；未配置时保持已有签到缓存。
	var checkinRes upstream.CheckinResult
	if acc.Upstream.HasCheckinStatusPath() {
		checkinRes, err = acc.Upstream.CheckinStatus(ctx)
		if err != nil {
			m.setError(acc, err)
			return acc, fmt.Errorf("account: 账号 %q 签到状态查询失败: %w", id, err)
		}
	}

	// 全部成功，刷新缓存状态
	m.mu.Lock()
	acc.Healthy = true
	acc.LastCheck = time.Now()
	acc.Error = ""
	acc.TotalBalance = float64(accRes.Account.Balance)
	acc.TempBalance = float64(accRes.Account.LimitedBalance)
	acc.PermanentBalance = float64(accRes.Account.PermanentBalance)
	acc.TempExpire = accRes.Account.NextExpiryAt
	// 上游状态查询返回有效签到日期时刷新签到缓存；
	// date 为空（当天已签、无新奖励的幂等响应）时保留已有缓存，避免误覆盖
	if checkinRes.Date != "" {
		acc.CheckedIn = checkinRes.CheckedIn
		acc.CheckinDate = checkinRes.Date
	}
	m.mu.Unlock()

	return acc, nil
}

// Checkin 触发指定账号签到，并刷新签到状态缓存。
// 只要上游返回 2xx 且没有显式业务错误，就视为签到请求已被接受。
// checkedIn=false 是上游幂等响应的常见值，不能据此判定签到失败；
// 上游 date 缺失时回退为配置时区的当天日期，保证网页能显示签到时间。
func (m *AccountManager) Checkin(id string) error {
	acc, err := m.Get(id)
	if err != nil {
		return err
	}
	res, err := acc.Upstream.Checkin(context.Background())
	if err != nil {
		m.setError(acc, err)
		return fmt.Errorf("account: 账号 %q 签到失败: %w", id, err)
	}

	date := res.Date
	if date == "" {
		date = time.Now().In(acc.Upstream.Location()).Format("2006-01-02")
	}
	m.mu.Lock()
	acc.CheckedIn = true
	acc.CheckinDate = date
	acc.Healthy = true
	acc.Error = ""
	acc.LastCheck = time.Now()
	m.mu.Unlock()
	return nil
}

// Pick 通过调度器从全部账号中选择一个账号。
// 将 accounts 转为 []AccountInfo 传入 scheduler.Pick，再按返回的 ID 取回账号。
func (m *AccountManager) Pick() (*Account, error) {
	m.mu.RLock()
	infos := make([]AccountInfo, 0, len(m.accounts))
	for _, a := range m.accounts {
		infos = append(infos, AccountInfo{
			ID:          a.ID,
			Identifier:  a.Identifier,
			TempBalance: a.TempBalance,
			Healthy:     a.Healthy,
		})
	}
	m.mu.RUnlock()

	id, err := m.scheduler.Pick(infos)
	if err != nil {
		return nil, err
	}
	return m.Get(id)
}

// CheckAll 检测全部账号，供启动时初始化健康状态与余额缓存。
// 单个账号失败不会阻止其它账号检测，返回失败账号数量。
func (m *AccountManager) CheckAll() int {
	failed := 0
	for _, acc := range m.List() {
		if _, err := m.Check(acc.ID); err != nil {
			failed++
		}
	}
	return failed
}

// SetSchedule 设置调度方式并写回 config.toml。
// fixed 模式必须提供非空 fixedID；写盘失败时恢复原内存配置。
func (m *AccountManager) SetSchedule(mode, fixedID string) error {
	if mode == ModeFixed && fixedID == "" {
		return fmt.Errorf("account: fixed 模式必须提供账号 ID")
	}
	old := m.scheduler.GetConfig()
	if err := m.scheduler.SetMode(mode); err != nil {
		return err
	}
	if fixedID != "" {
		if err := m.scheduler.SetFixed(fixedID); err != nil {
			return err
		}
	}
	cfg := m.scheduler.GetConfig()
	if fixedID == "" {
		cfg.FixedID = ""
	}
	if err := m.store.SaveSchedule(cfg); err != nil {
		_ = m.scheduler.SetMode(old.Mode)
		if old.FixedID != "" {
			_ = m.scheduler.SetFixed(old.FixedID)
		}
		return err
	}
	return nil
}

// GetSchedule 返回当前调度配置。
func (m *AccountManager) GetSchedule() config.ScheduleConfig {
	return m.scheduler.GetConfig()
}

// saveAccountsLocked 把当前账号列表写回 config.toml。调用方需持有写锁。
func (m *AccountManager) saveAccountsLocked() error {
	authAccounts := make([]config.AuthAccount, 0, len(m.accounts))
	for _, a := range m.accounts {
		authAccounts = append(authAccounts, config.AuthAccount{
			Identifier:  a.Identifier,
			Password:    a.Password,
			AutoRelogin: a.AutoRelogin,
		})
	}
	return m.store.SaveAccounts(authAccounts)
}

// setError 记录账号错误并标记不健康。调用方无需持有锁。
func (m *AccountManager) setError(acc *Account, err error) {
	m.mu.Lock()
	acc.Healthy = false
	acc.Error = err.Error()
	acc.LastCheck = time.Now()
	m.mu.Unlock()
}

// setHealthy 标记账号健康并清空错误。调用方无需持有锁。
func (m *AccountManager) setHealthy(acc *Account, healthy bool) {
	m.mu.Lock()
	acc.Healthy = healthy
	if healthy {
		acc.Error = ""
	}
	acc.LastCheck = time.Now()
	m.mu.Unlock()
}
