# 安装与使用

如果你只是想用，不打算自己开发 Mod，先记住这几件事就够了。

## 更推荐和 Next 一起使用

LongLive Lib 建立在 `BepInEx` 和 `Next` 这套生态上。

理论上它可以不依赖 Next 单独运行，但从兼容性和实际体验来看，还是更建议一起用。

## 首次进入游戏时加载慢一点很正常

LongLive Lib 会多加载一些运行时内容。

所以第一次进游戏，或者更新之后重新进游戏时，比平时慢一点是正常现象。

## 配置入口在 F1

如果你想自己决定某个功能开不开，可以在 `F1` 的 Mod 配置里看这些项目：

- `EnableBulkItemUseOptimization`
- `EnablePopTipOptimization`
- `EnableExperimentalBattleGuard`
- `EnableFadeOptimization`

## 想确认有没有正确加载

可以看主菜单里的 `LongLive Lib` 入口。

如果入口正常出现，说明这套 Host 基本已经起来了。

如果点进去还能看到状态信息，通常就说明基础部分已经在工作了。

## 遇到问题先看哪里

如果你遇到的是“功能没生效”“界面没出现”“还是很卡”这类问题，可以先看：

- [创意工坊说明](../workshop/overview.md)
- [上传清单](../workshop/upload-checklist.md)

如果你是在本地自己部署，也可以参考：

- [部署指南](../guide/deploy-guide.md)

## 哪些功能可以自己关

如果你不喜欢某一类优化，通常可以在 `F1` 里直接关掉。常见的开关有：

- `EnableBulkItemUseOptimization`
- `EnablePopTipOptimization`
- `EnableExperimentalBattleGuard`
- `EnableFadeOptimization`
