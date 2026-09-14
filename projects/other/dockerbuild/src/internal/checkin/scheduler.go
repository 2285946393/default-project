// Package checkin 负责每日签到调度。
package checkin

import (
	"context"
	"fmt"
	"log"
	"sync"
	"time"

	"clodesen2api/internal/upstream"
)

// Client 描述签到调度所需的最小上游能力。
type Client interface {
	Checkin(context.Context) (upstream.CheckinResult, error)
}

// Scheduler 负责每天在指定时区的指定时刻触发一次签到。
// 通过 lastDate 保证“每天只签到一次”；进程重启会重置状态并立即补签当天。
type Scheduler struct {
	up  Client
	at  string // "HH:MM"
	loc *time.Location

	stopOnce sync.Once
	stopCh   chan struct{}

	mu       sync.Mutex
	lastDate string // YYYY-MM-DD，最近一次成功签到的日期
}

// NewScheduler 构造调度器。不立即触发，需调用 Run。
func NewScheduler(up Client, at, tz string) (*Scheduler, error) {
	if at == "" {
		at = "04:00"
	}
	if _, err := time.Parse("15:04", at); err != nil {
		return nil, fmt.Errorf("invalid checkin.at %q: %w", at, err)
	}
	if tz == "" {
		tz = "Asia/Shanghai"
	}
	loc, err := time.LoadLocation(tz)
	if err != nil {
		return nil, fmt.Errorf("load timezone %q: %w", tz, err)
	}
	return &Scheduler{
		up:     up,
		at:     at,
		loc:    loc,
		stopCh: make(chan struct{}),
	}, nil
}

// Run 启动调度协程。先立即尝试补签当天（若当天还没签过），再每天定时触发。
func (s *Scheduler) Run(ctx context.Context) {
	// 启动补签：进程刚起来，如果今天还没签到，先签一次
	s.tryOnce(ctx, "boot")

	ticker := time.NewTicker(30 * time.Second)
	defer ticker.Stop()

	for {
		select {
		case <-ctx.Done():
			log.Printf("[checkin] scheduler stopped")
			return
		case <-s.stopCh:
			log.Printf("[checkin] scheduler stopped")
			return
		case now := <-ticker.C:
			s.maybeTick(ctx, now)
		}
	}
}

// Stop 停止调度。
func (s *Scheduler) Stop() {
	s.stopOnce.Do(func() { close(s.stopCh) })
}

// maybeTick 到点且当天未签到时触发一次。
func (s *Scheduler) maybeTick(ctx context.Context, nowGlobal time.Time) {
	now := nowGlobal.In(s.loc)

	// 解析今日目标时刻
	hm, err := time.Parse("15:04", s.at)
	if err != nil {
		return
	}
	target := time.Date(now.Year(), now.Month(), now.Day(),
		hm.Hour(), hm.Minute(), 0, 0, s.loc)

	// 还没到点，跳过
	if now.Before(target) {
		return
	}
	// 已过点 + 今天没签 -> 补签
	s.tryOnce(ctx, "scheduled")
}

// tryOnce 执行一次签到，仅当今天还没成功签到时。
func (s *Scheduler) tryOnce(ctx context.Context, source string) {
	today := todayIn(s.loc)

	s.mu.Lock()
	last := s.lastDate
	s.mu.Unlock()
	if last == today {
		return
	}

	log.Printf("[checkin] trigger (%s) at %s", source, time.Now().In(s.loc).Format(time.RFC3339))
	res, err := s.up.Checkin(ctx)
	if err != nil {
		log.Printf("[checkin] failed: %v", err)
		return
	}

	// 以响应里的 date 为准；缺失则回退到本地今天。
	// checkedIn=false 可能只是上游的幂等结果，不能阻止当天重试状态。
	date := res.Date
	if date == "" {
		date = today
	}

	s.mu.Lock()
	s.lastDate = date
	s.mu.Unlock()
	log.Printf("[checkin] success date=%s reward=%d balance=%d checkedIn=%v",
		date, res.Reward, res.Balance, res.CheckedIn)
}

// todayIn 返回指定时区下今天的 YYYY-MM-DD。
func todayIn(loc *time.Location) string {
	return time.Now().In(loc).Format("2006-01-02")
}
