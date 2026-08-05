# HY2 (Hysteria 2) 支持设计文档

## 背景

ProxyClient 当前支持 VMess / VLESS / Trojan / Shadowsocks 四种协议。用户希望增加对 Hysteria 2（HY2）协议的支持，因为该协议目前较为流行，且能提供较好的抗封锁和带宽性能。

## 目标

在 ProxyClient 中完整支持 HY2 协议，包括：

- 解析 `hysteria2://` 分享链接
- 手动添加和编辑 HY2 节点
- 生成 Xray 可用的 `hysteria2` outbound 配置
- 与现有系统代理、测速、托盘等功能无缝集成

## 方案

采用方案 A：完整支持。

## 数据模型变更

### Protocols 常量

在 `ProxyClient.Models.Protocols` 中新增：

```csharp
public const string Hysteria2 = "hysteria2";
```

### ServerItem 新增字段

在 `ServerItem` 中增加 HY2 专属字段：

| 字段 | 类型 | 含义 |
|------|------|------|
| `Hy2Password` | string | 认证密码（对应 auth） |
| `Hy2Obfs` | string | 混淆类型（如 `salamander`） |
| `Hy2ObfsPassword` | string | 混淆密码 |
| `Hy2UpMbps` | string | 上传带宽（如 `100 Mbps`） |
| `Hy2DownMbps` | string | 下载带宽（如 `100 Mbps`） |
| `Hy2DisableUdp` | bool | 是否禁用 UDP 转发（默认 false） |

> 注：带宽字段使用 string 类型，方便用户直接输入 `100 Mbps` 或 `100`，生成配置时再处理单位。

## 分享链接解析

### URL 格式

支持如下常见格式：

```
hysteria2://password@host:port?obfs=salamander&obfs-password=xxx&sni=xxx&insecure=1&upmbps=100&downmbps=100#remark
```

### 解析逻辑

在 `ShareLinkParser` 中新增 `ParseHysteria2` 方法：

- 使用 `Uri` 解析地址和端口
- 从 `UserInfo` 提取密码
- 从 Query 解析：`obfs`、`obfs-password`、`sni`、`insecure`、`upmbps`、`downmbps`
- 从 Fragment 解析备注
- 默认 `Network = "udp"`，`StreamSecurity = "tls"`

## Xray 配置生成

### Outbound

在 `XrayConfigBuilder.BuildProxyOutbound` 中增加 `case Protocols.Hysteria2`，生成如下结构：

```json
{
  "tag": "proxy",
  "protocol": "hysteria2",
  "settings": {
    "servers": [
      {
        "address": "host",
        "port": 443
      }
    ],
    "auth": "password",
    "obfs": "salamander",
    "obfsPassword": "obfs-password",
    "bandwidth": {
      "up": "100 Mbps",
      "down": "100 Mbps"
    },
    "disableUdp": false
  },
  "streamSettings": {
    "security": "tls",
    "tlsSettings": {
      "serverName": "sni",
      "allowInsecure": false
    }
  }
}
```

### Inbound

HY2 出站使用本地 SOCKS/HTTP 入口，保持现有 `inbounds` 不变。

## UI 变更

### ServerEditWindow

1. 协议下拉框增加 `hysteria2` 选项
2. 基础信息区域保持：备注、协议、地址、端口
3. 当协议为 `hysteria2` 时：
   - 隐藏：alterId、加密方法、传输、Path、Host、Flow、Reality 公钥、ShortId
   - 显示：密码、obfs 类型、obfs 密码、上传带宽、下载带宽、禁用 UDP、SNI、跳过证书验证
4. 其他协议保持现有字段不变

### 主界面

- 节点列表中的协议标签自动显示 `hysteria2`
- 其他交互逻辑无需改动

## 测试计划

1. 使用示例 `hysteria2://` 链接测试解析
2. 手动添加 HY2 节点并编辑保存
3. 启动核心，验证生成的 `config.json` 包含正确的 `hysteria2` outbound
4. 验证核心启动不因为配置错误而退出
5. 验证系统代理、测速、托盘等功能正常

## 兼容性

- 现有 VMess / VLESS / Trojan / Shadowsocks 节点数据不受影响
- 新增字段为可选，旧数据反序列化后默认空值

## 备注

- 依赖 Xray-core 版本支持 `hysteria2` 协议（当前 xray-core 版本为 26.3.27，已支持）
- 带宽单位为 `Mbps`，若用户输入纯数字，自动追加 `Mbps`
