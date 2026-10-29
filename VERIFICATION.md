# 修改版验证记录

以下功能验证来自本机此前修补过程；此次上传没有重新执行会改变桌面或应用数据的交互操作。

| 范围 | 已完成的检查 |
| --- | --- |
| 桌面层 | 确认固定桌面层和关闭自动隐藏的设置生效，Dock 位于普通应用窗口下方。 |
| 运行指示点 | 12 个初始创建及刷新状态用例，覆盖应用运行/未运行与指示点设置。 |
| 悬浮放大 | 检查三个内置入口及应用条的放大常量，实际峰值为 1.38。 |
| 首帧玻璃 | 实际应用的四次打开检查，包含隐藏超过 12 秒后打开及快速取消；可见首帧材质均已准备好，没有记录到缺失材质的可见渲染，后续转入实时背景。准备耗时约 51–390 ms。 |
| 未满页分页 | 14 项回归检查，含独立数据目录下的真实 LauncherWorkspace 会话、提交与重新加载；27 个应用分成 [26, 1] 两页。未重排本机实际布局。 |
| 快捷方式 | 检查包注册身份、Shell PIDL 目标、AppUserModelId 以及 GlassDesk 窗口匹配；GPT 匹配 ChatGPT.exe，Claude 未误匹配 Explorer。 |
| 删除区域 | 在 1050、720、600 DIP 窗口宽度下检查 38×29 DIP 区域，不与应用网格或搜索框重叠，隐藏后恢复搜索栏边距。 |
| 内置操作收起启动台 | 使用隔离加载上下文检查已有/关闭启动台两种状态，确认原桌面操作回调仍执行；检查“此电脑”处理函数可 JIT。安装后验证更新 DLL 已加载，并通过本地 IPC 检查启动台打开/收起。 |

## 本次归档检查

- 四个修改程序集的 SHA-256 与完成修补时的记录一致。
- 运行文件清单为 `runtime-manifest.json`，不包含日志、缓存和个人应用数据。
- 全部单文件均低于 GitHub 100 MiB 文件限制。
- 归档前检查上传文本中的常见凭据格式；不上传 Git 凭据或代理配置。

在仓库根目录使用 PowerShell 校验运行文件：

```powershell
$manifest = Get-Content -LiteralPath ./runtime-manifest.json -Raw | ConvertFrom-Json
foreach ($entry in $manifest.files) {
    $actual = (Get-FileHash -LiteralPath $entry.path -Algorithm SHA256).Hash
    if ($actual -ne $entry.sha256) { throw "Hash mismatch: $($entry.path)" }
}
```

可复现的修补/探针源码位于 `Data/recovery/`，包含 `PagingRegression.cs`、`IndicatorProbe.cs`、`InspectLayout.cs`、`InspectBehavior.cs` 和 `WindowMatchProbe.cs` 等。部分历史工具使用本机路径，需要按工作目录调整。
