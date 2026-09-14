// Package httpx 提供跨包共享的轻量 HTTP 工具函数。
package httpx

import (
	"net/http"
	"strings"
)

// CopyHeaders 把上游响应头复制到下游响应，跳过自动管理的 hop-by-hop / 长度头。
func CopyHeaders(dst, src http.Header) {
	for k, vs := range src {
		switch strings.ToLower(k) {
		case "content-length", "transfer-encoding", "connection", "keep-alive":
			continue
		}
		for _, v := range vs {
			dst.Add(k, v)
		}
	}
}
