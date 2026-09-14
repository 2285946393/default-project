// clodesen2api 是应用启动入口，负责组装各层依赖并管理进程生命周期。
package main

import (
	"context"
	"errors"
	"log"
	"net/http"
	"os"
	"os/signal"
	"path/filepath"
	"syscall"
	"time"

	"github.com/lfhy/flag"

	"clodesen2api/internal/account"
	"clodesen2api/internal/checkin"
	"clodesen2api/internal/config"
	"clodesen2api/internal/server"
)

func main() {
	cfg := config.Load()

	// 配置文件路径（-c 指定或默认 config.toml），password.json / key.json 与其同目录
	configPath := flag.GetConfig().GetConfigPath()

	// 创建账号配置存储与调度器
	store := account.NewConfigStore(configPath)
	scheduleCfg, err := config.LoadSchedule(configPath)
	if err != nil {
		log.Printf("[boot] 读取调度配置失败(使用默认 random): %v", err)
	}
	scheduler := account.NewScheduler(scheduleCfg)

	// 创建多账号管理器：从 config.toml 读取全部账号并为每个账号创建独立 Upstream
	mgr, err := account.NewAccountManager(store, scheduler, cfg)
	if err != nil {
		log.Fatalf("[fatal] 初始化账号管理器失败: %v", err)
	}

	// 创建密码存储与 Key 存储（与 config.toml 同目录）
	dir := filepath.Dir(configPath)
	pw, err := account.NewPasswordStore(filepath.Join(dir, "password.json"))
	if err != nil {
		log.Fatalf("[fatal] 初始化密码存储失败: %v", err)
	}
	ks, err := account.NewKeyStore(filepath.Join(dir, "key.json"))
	if err != nil {
		log.Fatalf("[fatal] 初始化 Key 存储失败: %v", err)
	}

	// 创建管理 API 处理器
	admin := server.NewAdminHandler(mgr, pw, ks)

	// 启动时完整检测全部账号，初始化健康状态和余额缓存，避免调度使用未初始化的零余额。
	accounts := mgr.List()
	if len(accounts) == 0 {
		log.Printf("[boot] 未配置任何账号，可通过管理后台 /api/admin/accounts 添加")
	} else {
		for _, acc := range accounts {
			log.Printf("[boot] 正在检测上游账号 %s ...", acc.Identifier)
		}
		failed := mgr.CheckAll()
		log.Printf("[boot] 账号检测完成：共 %d 个，失败 %d 个", len(accounts), failed)
	}

	rootCtx, rootCancel := context.WithCancel(context.Background())
	defer rootCancel()

	// 启动每日签到调度（checkin.enable=true 时，为每个账号分别调度）
	if cfg.Checkin.Enable {
		if len(accounts) == 0 {
			log.Printf("[boot] 签到已跳过 (无账号)")
		} else {
			for _, acc := range accounts {
				sch, err := checkin.NewScheduler(acc.Upstream, cfg.Checkin.At, cfg.Checkin.Timezone)
				if err != nil {
					log.Printf("[boot] 账号 %s 签到调度初始化失败: %v", acc.Identifier, err)
					continue
				}
				log.Printf("[boot] 账号 %s 签到调度已启动，时区=%s 每日 %s", acc.Identifier, cfg.Checkin.Timezone, cfg.Checkin.At)
				go sch.Run(rootCtx)
				defer sch.Stop()
			}
		}
	} else {
		log.Printf("[boot] 签到已跳过 (enable=%v)", cfg.Checkin.Enable)
	}

	srv := &http.Server{
		Addr:              cfg.Server.Addr,
		Handler:           server.NewHandler(mgr, ks, admin),
		ReadHeaderTimeout: 15 * time.Second,
	}

	// 优雅退出
	go func() {
		sigCh := make(chan os.Signal, 1)
		signal.Notify(sigCh, syscall.SIGINT, syscall.SIGTERM)
		<-sigCh
		log.Printf("[shutdown] 收到退出信号，等待请求结束...")
		rootCancel()
		ctx, cancel := context.WithTimeout(context.Background(), 10*time.Second)
		defer cancel()
		_ = srv.Shutdown(ctx)
	}()

	if len(accounts) > 0 {
		log.Printf("[boot] clodesen2api listening on %s -> %s (OpenAI-compatible)", cfg.Server.Addr, accounts[0].Upstream.OpenAIBaseURL())
	} else {
		log.Printf("[boot] clodesen2api listening on %s (OpenAI-compatible)", cfg.Server.Addr)
	}
	if err := srv.ListenAndServe(); err != nil && !errors.Is(err, http.ErrServerClosed) {
		log.Fatalf("[fatal] server error: %v", err)
	}
	log.Printf("[shutdown] bye")
}
