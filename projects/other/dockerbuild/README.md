# clodesen2api Docker 部署包

本目录是 `clodesen2api`（OpenAI 兼容网关）的完整 Docker 部署包，可整体拷贝到任意装有 Docker 的 Windows 电脑后直接使用。

## 目录结构

```
dockerbuild/
├── Dockerfile          # 多阶段构建（golang:1.23-alpine 构建 → alpine:3.20 运行）
├── compose.yaml        # docker compose 编排（build context 为本目录）
├── .dockerignore       # 构建上下文排除规则（config/ 不进镜像）
├── README.md           # 本说明
├── config/             # 宿主机配置目录（挂载为容器内 /config，可写）
│   ├── config.toml     # 主配置：多账号 [[auth]] + [schedule] 调度
│   └── password.json   # 管理后台登录密码（默认 qq971971）
│   └── key.json        # 网关 Key（首次启动自动生成，无需手动创建）
└── src/                # 完整源码（go.mod、go.sum、cmd/、internal/、web/）
```

> 说明：`config/` 目录通过 compose 挂载为容器内 `/config`，**不会被打进镜像**，避免账号密码等敏感信息泄露。`key.json` 首次启动时由程序自动生成随机 Key 并写入 `/config/key.json`。

## 部署步骤

1. 将本目录整体拷贝到目标电脑（例如 `D:\dockerbuild`）。

2. `config/config.toml` 默认不预置任何账号（`[[auth]]` 为空），无需手动编辑。首次启动后访问 <http://127.0.0.1:3003>，登录（默认密码 `qq971971`），在网页中通过 **“添加账号”** 按钮添加账号，账号会自动写入 `config/config.toml` 的 `[[auth]]` 段。

3. 在 `dockerbuild` 目录下构建并启动：

   ```powershell
   docker compose up -d --build
   ```

4. 查看状态与日志：

   ```powershell
   docker compose ps
   docker compose logs -f
   ```

## 访问方式

- 管理后台：<http://127.0.0.1:3003>，默认登录密码 `qq971971`（可在后台修改，会写回 `config/password.json`）。
- 健康检查：<http://127.0.0.1:3003/healthz>

服务仅绑定到本机 `127.0.0.1:3003`，容器内监听 `:8080`。

## 账号管理

`config/config.toml` 的 `[[auth]]` 段由网页账号管理系统**自动管理**，无需手动编辑（除非需要预置账号）：

- 登录管理后台后，在网页中通过 **“添加账号”** 按钮添加账号，账号会自动写入 `config/config.toml` 的 `[[auth]]` 段，无需重启即可生效。
- 在网页中删除账号时，对应 `[[auth]]` 条目也会自动从 `config/config.toml` 移除。
- 如需预置账号（例如批量部署），可手动在 `config/config.toml` 的 `[[auth]]` 段填写 `identifier` / `password` / `auto_relogin`，修改后执行 `docker compose restart` 生效。

## 网关调用示例

网关为 OpenAI 兼容接口，调用 `/v1/*` 需携带 Key（首次启动后查看 `config/key.json` 中的 `key` 字段）。

使用 `X-API-Key` 请求头：

```powershell
curl http://127.0.0.1:3003/v1/chat/completions `
  -H "Content-Type: application/json" `
  -H "X-API-Key: <你的key>" `
  -d "{\"model\":\"gpt-3.5-turbo\",\"messages\":[{\"role\":\"user\",\"content\":\"你好\"}]}"
```

或使用 `Authorization: Bearer`：

```powershell
curl http://127.0.0.1:3003/v1/chat/completions `
  -H "Content-Type: application/json" `
  -H "Authorization: Bearer <你的key>" `
  -d "{\"model\":\"gpt-3.5-turbo\",\"messages\":[{\"role\":\"user\",\"content\":\"你好\"}]}"
```

## 日常运维

```powershell
# 查看状态
docker compose ps

# 查看日志（-f 持续跟踪）
docker compose logs -f

# 重启
docker compose restart

# 停止并删除容器（保留 config/ 数据）
docker compose down

# 停止并删除容器 + 网络（保留 config/ 数据）
docker compose down --remove-orphans
```

修改 `config/config.toml` 后需重启生效：

```powershell
docker compose restart
```

## 更新代码 / 前端页面后的重新部署

修改了 `src/` 下的 Go 代码或 `src/web/index.html` 后，**必须重建镜像**，`docker compose restart` 不会重新编译，也不会更新镜像里的 `web/index.html`：

```powershell
docker compose build --no-cache
docker compose up -d --force-recreate
```

说明：

- `--no-cache` 强制重新执行 `COPY src/ .` 与 `go build`，避免 Docker 层缓存导致改动没进镜像。
- `--force-recreate` 强制用新镜像重建容器，避免容器仍跑旧镜像。
- 程序在启动时会把 `web/index.html` 一次性读入内存缓存（见 [`loadIndexHTML()`](src/internal/server/handler.go:109)），所以更新前端后必须重建/重启容器，改文件本身不会热生效。
- 页面响应已带 `Cache-Control: no-cache, no-store, must-revalidate`（见 [`handleIndex()`](src/internal/server/handler.go:120)），正常情况下浏览器不会再用旧缓存。若仍看到旧页面，用 <kbd>Ctrl</kbd>+<kbd>F5</kbd> 强制刷新。

## 端口修改

默认映射 `127.0.0.1:3003:8080`。如需修改宿主机端口，编辑 `compose.yaml`：

```yaml
ports:
  - "127.0.0.1:3003:8080"   # 改为 127.0.0.1:新端口:8080
```

然后重新执行 `docker compose up -d --build`。

## ARM 架构说明

默认构建目标为 Linux `amd64`（`Dockerfile` 中 `GOARCH=amd64`）。若目标主机为 ARM 架构（如 Apple Silicon、ARM Windows），请将 `Dockerfile` 中的 `GOARCH` 改为 `arm64`：

```dockerfile
RUN CGO_ENABLED=0 GOOS=linux GOARCH=arm64 \
    go build -trimpath -ldflags="-s -w" \
    -o /out/clodesen2api ./cmd/clodesen2api
```

## 首次启动自动初始化

- `config.toml` 不存在时：程序会从内置模板创建（本包已预置 `config/config.toml`）。
- `password.json` 不存在时：程序自动创建并写入默认密码 `qq971971`（本包已预置）。
- `key.json` 不存在时：程序自动生成 32 位随机 Key 并写入 `/config/key.json`（本包不预置，避免空 key 报错）。
