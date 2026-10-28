# iOS の App Store 公開

App Store に iOS 版を出すための手順と、App Store Connect に入れる内容をまとめます。

## アプリ側で設定済みのこと

| 項目 | 内容 | 場所 |
|---|---|---|
| Bundle ID | `com.kenmro.kakeiboapp`（iOS だけ。Android は `com.companyname.kakeiboapp` のまま） | `KakeiboApp.csproj` |
| バージョン | `ApplicationDisplayVersion`（例 2.10）と `ApplicationVersion`（ビルド番号、例 12）。Android と共通 | `KakeiboApp.csproj` |
| Release の署名 | 実機（`ios-arm64`）向けに `Apple Distribution` 証明書で署名 | `KakeiboApp.csproj` |
| 対応 OS | iOS 15.0 以上、iPhone と iPad | `KakeiboApp.csproj`、`Platforms/iOS/Info.plist` |
| アイコン・起動画面 | ぶたの貯金箱のアイコン（1024px を含む）と起動画面 | `Resources/AppIcon`、`Resources/Splash` |
| 画面の仕組み | iOS 27 で必須の UIScene に対応 | `Platforms/iOS/SceneDelegate.cs`、`Info.plist` |
| Face ID の説明文 | 「家計簿を開くときの本人確認に Face ID を使います。」 | `Info.plist` |
| 輸出コンプライアンス | `ITSAppUsesNonExemptEncryption` = false（独自の暗号化なし） | `Info.plist` |
| プライバシーマニフェスト | トラッキングなし、収集するデータなし。使っている API の理由（ファイルの日時、起動からの時間、空き容量、設定の保存） | `Platforms/iOS/Resources/PrivacyInfo.xcprivacy` |
| プライバシーポリシー | [PRIVACY.md](PRIVACY.md) | `docs/PRIVACY.md` |
| 利用規約 | [TERMS.md](TERMS.md)。App Store Connect の使用許諾契約は Apple の標準のままにします | `docs/TERMS.md` |
| アプリ内のリンク | 設定の「このアプリについて」に、利用規約・プライバシーポリシー・お問い合わせ | `Views/SettingPage.xaml`、`Services/AppLinks.cs` |
| 外部への送信なし | Syncfusion のコントロールが持つ利用状況の送信（Application Insights へ端末名などを送る）を起動時に止めています | `MauiProgram.cs` |

## 審査ガイドラインとの対応

[App Review Guidelines](https://developer.apple.com/jp/app-store/review/guidelines/) のうち、このアプリに関係するものです。

| ガイドライン | 求められていること | このアプリ |
|---|---|---|
| 1.5 開発者情報 | アプリとサポート URL に、連絡の手段を入れる | 設定の「お問い合わせ」、サポート URL は GitHub の Issues |
| 2.1 完全性 | 仮の文言や動かないリンクを残さない。実機で落ちない | Face ID の説明文などを本番の文言に。リンクはすべて公開済みのページ。TestFlight で実機を確認する |
| 2.3 正確なメタデータ | スクリーンショットと説明文が実際のアプリと合う | 下の「ストアの表示」とスクリーンショットを実際の画面から作る |
| 3.1.2 サブスクリプション | 自動更新のサブスクリプションには利用規約へのリンクが要る | 課金なしのため対象外。利用規約は任意でアプリ内に置いています |
| 3.2.1(viii) 金融 | お金の管理（送金・投資など）を行うアプリは金融機関が出す | 記録と計算だけで、お金を動かさないため対象外。利用規約に「金融サービスや助言ではない」と記載 |
| 5.1.1(i) プライバシーポリシー | App Store Connect とアプリ内の両方にリンクを置く。集める情報、第三者との共有、保存期間と削除の方法を書く | 設定の「プライバシーポリシー」。PRIVACY.md にすべて記載 |
| 5.1.1(v) アカウント | アカウントを作れるなら、アプリ内で削除もできる | アカウントなしのため対象外 |
| 5.1.2 データの利用と共有 | トラッキングには許可（ATT）が要る | トラッキングなし。プライバシーマニフェストにも記載 |
| 5.1.1 データの収集 | App Store Connect の「App のプライバシー」を正しく答える | 収集なし。Syncfusion の送信を止めたので「データを収集しない」で正しい |

## 最初の1回だけ

1. **App ID を登録する**：[Apple Developer](https://developer.apple.com/account/resources/identifiers/list) の「Identifiers」で、App IDs を「Explicit」の `com.kenmro.kakeiboapp` で登録します。Capabilities は追加しなくてかまいません（Face ID に設定は要りません）。
2. **配布用のプロファイルを作る**：「Profiles」で「App Store Connect」を選び、上の App ID と Apple Distribution 証明書で作成します。名前は例えば `KakeiboApp App Store` にします。ダウンロードしてダブルクリックすると、この Mac に入ります。
3. **App Store Connect にアプリを作る**：[App Store Connect](https://appstoreconnect.apple.com/apps) の「＋」→「新規 App」で、次のように入れます。
   - プラットフォーム：iOS
   - 名前：App Store で表示する名前（ほかのアプリと重ならないもの。下の「ストアの表示」を参照）
   - プライマリ言語：日本語
   - バンドル ID：`com.kenmro.kakeiboapp`
   - SKU：例 `kakeiboapp-ios`

## 毎回のリリース

1. **バージョンを上げる**：`KakeiboApp.csproj` の `ApplicationVersion` を必ず上げます（同じビルド番号は2回アップロードできません）。表示用のバージョンを変えるときは `ApplicationDisplayVersion` も上げます。
2. **Syncfusion のライセンスキーを確認する**：`Repository/Constants.cs` の `SYNCFUSIOHN_LICENSE_KEY` に本物のキーが入っていることを確認します（このファイルはコミットしません）。
3. **IPA を作る**：

   ```bash
   dotnet publish KakeiboApp.csproj -f net10.0-ios -c Release -p:CodesignProvision="KakeiboApp App Store"
   ```

   `bin/Release/net10.0-ios/ios-arm64/publish/KakeiboApp.ipa` ができます。
4. **アップロードする**：Mac App Store の [Transporter](https://apps.apple.com/jp/app/transporter/id1450874784) に IPA をドラッグして「配信」します。数分〜数十分で App Store Connect の「TestFlight」にビルドが出ます。
5. **TestFlight で確かめる**：実機で起動、使い方、記録の追加、Face ID、バックアップと復元を確認します。シミュレータでは確認できないタップの操作（「＋ つける」のスライド、数字キーボードの「次へ」「完了」）も、ここで確かめます。
6. **審査に出す**：App Store Connect のバージョンのページでビルドを選び、「審査用に追加」→「審査へ提出」します。

## App Store Connect に入れる内容

### アプリのプライバシー

- プライバシーポリシーの URL：`https://github.com/ken-mro/KakeiboApp/blob/master/docs/PRIVACY.md`
- データの収集：「いいえ、このアプリからデータを収集しません」を選びます（記録は端末の中だけに保存され、外部へ送りません）。

### App 情報

- カテゴリ：ファイナンス
- 年齢制限指定：質問はすべて「なし」で、4+ になります。
- コンテンツの権利：第三者のコンテンツは含みません（フォントの Zen Maru Gothic は SIL Open Font License）。

### 価格と配信状況

- 価格：無料（有料にする場合は有料 App 契約が必要です）

### バージョン情報

- サポート URL：`https://github.com/ken-mro/KakeiboApp/issues`
- 著作権：例 `2026 ken_mro`
- スクリーンショット：iPhone は 6.9 インチ（1320×2868 など）、iPad は 13 インチ（2064×2752 など）が必要です。シミュレータの「iPhone 17 Pro Max」と「iPad Pro 13-inch (M5)」で、記録を入れた画面を撮ると、そのままの大きさで使えます。
- App Review に関する情報：ログインはありません。メモ欄に例えば「アカウントは不要です。生体認証は設定で任意にオンにする機能で、オフのままでもすべての機能を使えます。」と書きます。

### ストアの表示（案）

- 名前：`家計簿 - 今月使えるお金がわかる`（30文字まで。「家計簿」だけはほかのアプリと重なるおそれがあります）
- サブタイトル：`残りのお金と1日あたりがひと目で`（30文字まで）
- キーワード：`家計簿,節約,予算,貯金,生活費,固定費,支出,お小遣い,やりくり,シンプル`（100文字まで）
- 説明文：

  ```text
  毎月の収入から貯金と固定費を引いた「今月使えるお金」の中で、生活費をやりくりするための家計簿です。

  ■ 残りのお金と1日あたりがひと目でわかる
  ホームに、今月あといくら使えるかと、1日あたりに使える金額を大きく表示します。日数と支出のバーで、今月のペースも確かめられます。

  ■ つけるのはかんたん
  「＋ つける」から、収入・貯金・固定費・特別出費・生活費をつけられます。ボタンから指を上にすべらせて、種類を選ぶこともできます。

  ■ 予算と貯金の切り崩し
  生活費の種類ごとに予算を決められます。先月までの貯金を目的ごとに見て、今月切り崩す金額も入れられます。

  ■ 特別出費は別枠
  旅行や冠婚葬祭などの大きな出費は、特別出費として残りのお金とは別に記録します。

  ■ 安心して使える
  記録は端末の中だけに保存し、外部へ送りません。Face ID・Touch ID でロックできます。バックアップのファイルを作って、機種変更のときに戻せます。
  ```
