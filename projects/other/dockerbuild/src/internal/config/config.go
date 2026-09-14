// Package config 负责加载并合并命令行 / 配置文件 / 环境变量。
package config

import (
	"fmt"
	"os"
	"time"

	"github.com/lfhy/flag"
	"github.com/pelletier/go-toml/v2"
)

// Config 由命令行 / 配置文件 / 环境变量统一注入。
// lfhy/flag 会按 命令行 > 环境变量 > 配置文件 > 默认值 的优先级合并。
type Config struct {
	Server struct {
		Addr string
	}
	Upstream struct {
		BaseURL        string
		OpenAIBaseURL  string
		LoginPath      string
		LoginTimeout   time.Duration
		RequestTimeout time.Duration
		UserAgent      string
	}
	Auth struct {
		Identifier  string
		Password    string
		AutoRelogin bool
	}
	Checkin struct {
		Enable     bool
		Path       string
		StatusPath string // 签到状态查询端点路径，空则不做状态查询（保持缓存）
		At         string // 每天签到时间，HH:MM
		Timezone   string
	}
	Account struct {
		Path string
	}
}

// AuthAccount 表示一个上游登录账号，对应 config.toml 中的 [[auth]] 数组元素。
type AuthAccount struct {
	Identifier  string `toml:"identifier"`
	Password    string `toml:"password"`
	AutoRelogin bool   `toml:"auto_relogin"`
}

// ScheduleConfig 表示账号调度策略，对应 config.toml 中的 [schedule] 段。
type ScheduleConfig struct {
	Mode    string `toml:"mode"`
	FixedID string `toml:"fixed_id"`
}

// Load 用 lfhy/flag 注册所有参数，支持 -c config.toml 统一载入。
func Load() *Config {
	c := &Config{}

	flag.StringConfigVar(&c.Server.Addr, "addr", "server", "addr", ":8080", "本地监听地址")

	flag.StringConfigVar(&c.Upstream.BaseURL, "base-url", "upstream", "base_url", "https://pai.zaiduyu.top", "上游站点基础地址（登录、签到等）")
	flag.StringConfigVar(&c.Upstream.OpenAIBaseURL, "openai-base-url", "upstream", "openai_base_url", "https://pai.zaiduyu.top/api/openai", "上游 OpenAI API 基础地址")
	flag.StringConfigVar(&c.Upstream.LoginPath, "login-path", "upstream", "login_path", "/api/user-auth", "登录端点路径")
	flag.DurationConfigVar(&c.Upstream.LoginTimeout, "login-timeout", "upstream", "login_timeout", 30*time.Second, "登录请求超时")
	flag.DurationConfigVar(&c.Upstream.RequestTimeout, "request-timeout", "upstream", "request_timeout", 0, "上游请求超时(0=不限)")
	flag.StringConfigVar(&c.Upstream.UserAgent, "user-agent", "upstream", "user_agent",
		"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
		"浏览器 UA")

	flag.StringConfigVar(&c.Auth.Identifier, "identifier", "auth", "identifier", "", "登录账号(邮箱/用户名)")
	flag.StringConfigVar(&c.Auth.Password, "password", "auth", "password", "", "登录密码")
	flag.BoolConfigVar(&c.Auth.AutoRelogin, "auto-relogin", "auth", "auto_relogin", true, "401 时是否自动重新登录")

	flag.BoolConfigVar(&c.Checkin.Enable, "checkin", "checkin", "enable", true, "是否启用每日自动签到(GET)")
	flag.StringConfigVar(&c.Checkin.Path, "checkin-path", "checkin", "path", "/api/checkin", "签到端点路径")
	flag.StringConfigVar(&c.Checkin.StatusPath, "checkin-status-path", "checkin", "status_path", "", "签到状态查询端点路径(空则不做状态查询，保持缓存)")
	flag.StringConfigVar(&c.Checkin.At, "checkin-at", "checkin", "at", "04:00", "每天签到时间(HH:MM)")
	flag.StringConfigVar(&c.Checkin.Timezone, "checkin-tz", "checkin", "timezone", "Asia/Shanghai", "签到所用时区")

	flag.StringConfigVar(&c.Account.Path, "account-path", "account", "path", "/api/account", "账号余额查询端点路径")

	flag.Parse()
	return c
}

// LoadAccounts 用 go-toml/v2 读取 config.toml 中的 [[auth]] 账号数组。
// 兼容旧式单值 [auth]（auth 为单个表而非数组），此时自动包装为单元素数组返回。
func LoadAccounts(path string) ([]AuthAccount, error) {
	data, err := os.ReadFile(path)
	if err != nil {
		return nil, err
	}

	// 先解码为通用 map，判断 auth 是数组还是单个表
	var raw map[string]any
	if err := toml.Unmarshal(data, &raw); err != nil {
		return nil, err
	}

	v, ok := raw["auth"]
	if !ok {
		// 未配置任何账号
		return nil, nil
	}

	switch v.(type) {
	case []any:
		// [[auth]] 数组
		var doc struct {
			Auth []AuthAccount `toml:"auth"`
		}
		if err := toml.Unmarshal(data, &doc); err != nil {
			return nil, err
		}
		return doc.Auth, nil
	case map[string]any:
		// 旧式单值 [auth]，包装为单元素数组
		var doc struct {
			Auth AuthAccount `toml:"auth"`
		}
		if err := toml.Unmarshal(data, &doc); err != nil {
			return nil, err
		}
		return []AuthAccount{doc.Auth}, nil
	default:
		return nil, fmt.Errorf("config: auth 字段类型不支持: %T", v)
	}
}

// LoadSchedule 用 go-toml/v2 读取 config.toml 中的 [schedule] 段。
// 未配置或 mode 为空时默认 mode="random"。
func LoadSchedule(path string) (ScheduleConfig, error) {
	cfg := ScheduleConfig{Mode: "random"}

	data, err := os.ReadFile(path)
	if err != nil {
		return cfg, err
	}

	var doc struct {
		Schedule ScheduleConfig `toml:"schedule"`
	}
	if err := toml.Unmarshal(data, &doc); err != nil {
		return cfg, err
	}
	if doc.Schedule.Mode != "" {
		cfg.Mode = doc.Schedule.Mode
	}
	cfg.FixedID = doc.Schedule.FixedID
	return cfg, nil
}
