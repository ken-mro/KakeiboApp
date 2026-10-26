# KakeiboApp

月ごとの収入・貯金・固定費・生活費・特別出費を記録する .NET MAUIの家計簿アプリです。

## ドキュメント

- [使い方](docs/HOW_TO.md)
- [計算式一覧](docs/FORMULAS.md)
- [UI刷新計画](docs/UI_REDESIGN_PLAN.md)
- [iOS の App Store 公開](docs/IOS_RELEASE.md)
- [プライバシーポリシー](docs/PRIVACY.md)

## 開発

- 必要なもの： .NET 10 SDKとMAUIワークロード
- ビルド： `dotnet build KakeiboApp.csproj -f net10.0-android`
- テスト： `dotnet test --project KakeiboApp.Tests/KakeiboApp.Tests.csproj`
