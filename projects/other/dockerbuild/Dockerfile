# syntax=docker/dockerfile:1

# ---------- 构建阶段 ----------
FROM golang:1.23-alpine AS builder

WORKDIR /build
RUN apk add --no-cache ca-certificates git

# 先拷贝依赖清单，利用 Docker 层缓存加速依赖下载
COPY src/go.mod src/go.sum ./
RUN go mod download

# 拷贝全部源码（含 web/ 目录）
COPY src/ .

# 运行测试
RUN go test ./...

# 静态编译，减小镜像体积
# 默认 GOARCH=amd64；ARM 主机请改为 arm64
RUN CGO_ENABLED=0 GOOS=linux GOARCH=amd64 \
    go build -trimpath -ldflags="-s -w" \
    -o /out/clodesen2api ./cmd/clodesen2api

# ---------- 运行阶段 ----------
FROM alpine:3.20

# 安装证书、时区数据与健康检查用 wget；创建非 root 用户 app
RUN apk add --no-cache ca-certificates tzdata wget \
    && addgroup -S app \
    && adduser -S -G app app

WORKDIR /app

# 可执行文件
COPY --from=builder /out/clodesen2api /app/clodesen2api

# 管理页面：handler.go 以相对路径 "web/index.html" 读取，
# 因此必须放到 /app/web/index.html（WORKDIR 为 /app）
COPY src/web/index.html /app/web/index.html

RUN chown -R app:app /app

USER app
EXPOSE 8080
ENTRYPOINT ["/app/clodesen2api"]
CMD ["-c", "/config/config.toml"]
