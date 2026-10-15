# 計算式一覧

アプリに表示される金額と、その計算方法をまとめたものです。使い方は [HOW_TO.md](HOW_TO.md) を参照してください。

- ホーム画面の計算は [Services/BudgetCalculator.cs](../Services/BudgetCalculator.cs) にまとまっています。
- テストは [KakeiboApp.Tests](../KakeiboApp.Tests) にあります。
- 式を変えるときは、このドキュメントとテストも合わせて更新してください。

## 前提

- **選択月**：画面上部で選んだ年月です。内部では「その月の1日」（例：2026/10/01）として扱います。
- **月の判定**：「選択月の」データとは、日付の年と月が選択月と一致するものです（`BudgetCalculator.IsInMonth`）。
- **「先月まで」**：日付が選択月の1日より前のデータすべてです。前月だけでなく、それ以前の全期間を含みます。
- **切り崩し**：貯金データのうち、金額がマイナスのものです。
  - 例：旅行の貯金から3,000円取り崩す → 名前「旅行」、金額−3,000の貯金データとして保存されます。

## 収入・貯金・固定費

| 表示 | 式 | コード |
|---|---|---|
| 収入の合計 | 選択月の収入の金額の合計 | `MainPageViewModel.MonthlyIncomeTotal` |
| 貯金の合計 | 選択月の貯金のうち、金額が0より大きいもの（切り崩しを除く）の合計 | `BudgetCalculator.SavingsInMonth`, `MainPageViewModel.MonthlySavingTotal` |
| 固定費の合計 | 選択月の固定費の金額の合計 | `MainPageViewModel.MonthlyFixedCostTotal` |

## 生活費

| 表示 | 式 | コード |
|---|---|---|
| 生活費の合計 | 種類ごとの「出費」の合計 | `MainPageViewModel.MonthlyVariableCostTotal` |
| （予算）・予算総額 | 種類ごとの「予算」の合計 | `MainPageViewModel.MonthlyBudgetTotal`、表の集計行 |
| 出費（種類ごと） | 選択月の生活費のうち、その種類のものの金額の合計 | `BudgetCalculator.BuildBudgetControlResults` |
| 予算（種類ごと） | 選択月に登録したその種類の予算。未登録なら0 | `BudgetCalculator.BuildBudgetControlResults` |
| 残りの予算 | 予算 − 出費 | [BudgetControlResult.RemainingBudget](../Models/BudgetControlResult.cs#L8) |
| 1日あたり | 下の表を参照 | [BudgetControlResult.GetDailySpending](../Models/BudgetControlResult.cs#L11) |

### 1日あたり

まず、残りの予算が0以下なら **0** です。それ以外は、選択月と今日の関係で決まります。

| 選択月 | 式 |
|---|---|
| 過去の月 | 残りの予算（日割りしない） |
| 今月 | 残りの予算 ÷（今月の日数 − 今日の日 + 1） |
| 未来の月 | 残りの予算 ÷ その月の日数 |

- 今月の場合、日数には今日を含みます。例：10月15日なら`31 − 15 + 1 = 17`日で割ります。
- 未来の月は、月の初日から使い始める前提で、その月の全日数で割ります。うるう年の2月は29日です。

## 先月までの残高

[BudgetCalculator.TotalRemainingUntilPreviousMonth](../Services/BudgetCalculator.cs#L51)

```
先月までの残高 = 収入の合計 − 貯金の合計 − 固定費の合計 − 生活費の合計
                （いずれも選択月より前の全期間）
```

- 貯金の合計には切り崩し（マイナスの金額）も含みます。つまり「貯金した額 − 取り崩した額」の正味の額が引かれます。
- 特別出費は含みません。
- 表示場所は設定によって変わります。
  - 「残りのお金を翌月に繰り越す」がオン：ホーム上部の **先月の繰り越し額**
  - オフ：残りのお金セクションの **先月までの貯蓄額（貯金除く）**

## 今月使えるお金

[BudgetCalculator.MonthlyUsableMoney](../Services/BudgetCalculator.cs#L92)

```
繰り越しオフ：今月使えるお金 = 収入の合計 − 貯金の合計 − 固定費の合計
繰り越しオン：今月使えるお金 = 先月までの残高 + 収入の合計 − 貯金の合計 − 固定費の合計
```

先月までの残高がマイナスの場合は、そのまま差し引かれます。

## 残りのお金

[BudgetCalculator.MonthlyRemainingTotal](../Services/BudgetCalculator.cs#L106)

```
残りのお金 = 今月使えるお金 − 生活費の合計 + 切り崩し
```

| 表示 | 式 |
|---|---|
| `(うち 切り崩し: ...)` | 選択月の切り崩し額の合計（`MainPageViewModel.MonthlyLivingOff`） |
| 文字色 | 残りのお金が0未満なら赤。それ以外はライトテーマで黒、ダークテーマで白 |

## 先月までの貯金の内訳

[BudgetCalculator.SavingsUntilPreviousMonth](../Services/BudgetCalculator.cs#L69)、[SavingResult](../Models/SavingResult.cs)

| 列・表示 | 式 |
|---|---|
| 内容 | 貯金の名前。同じ名前の貯金を1行にまとめます |
| 金額 | 選択月より前の、その名前の貯金の合計（切り崩しを差し引いた正味） |
| 切り崩し | 選択月にその貯金から取り崩す額（正の数で表示・入力） |
| 貯金総額 | 各行の「金額 − 切り崩し」の合計 |

- 金額の合計が0の貯金は表示しません（全額を取り崩し終えた貯金など）。
- 切り崩しは、日付が選択月の1日ちょうどの、マイナスの貯金データと名前で結び付けます。
- 切り崩しに入力できるのは、0以上で、その行の金額以下の値だけです。

## 特別出費

| 表示 | 式 | コード |
|---|---|---|
| ホームの特別出費の合計 | 選択月の特別出費の金額の合計 | `MainPageViewModel.MonthlySpecialExpenseTotal` |
| 特別出費画面の`総額` | 選択年の特別出費の金額の合計 | [SpecialExpenseDetailPage.xaml](../Views/SpecialExpenseDetailPage.xaml#L80) |
| 追加時の日付の初期値 | 選択月が今月なら今日、それ以外は選択月の1日 | `BudgetCalculator.DefaultSpecialExpenseDate` |

## 生活費の一覧画面

| 表示 | 式 |
|---|---|
| 一覧の対象 | 選択月の1日以上、翌月1日未満の日付の生活費 |
| グループ見出し `種類: 金額` | その種類の生活費の金額の合計（[DetailPage.xaml](../Views/DetailPage.xaml#L82)） |

## 先月からコピー（固定費）

[BudgetCalculator.CopyFixedCostsFromPreviousMonth](../Services/BudgetCalculator.cs#L112)

- 選択月の前月（1月なら前年の12月）の固定費を、選択月の新しい行として追加します。
- 内容・金額・メモは同じで、日付だけ選択月の1日になります。

## 注意点（現在の仕様）

- **特別出費は「先月までの残高」「今月使えるお金」「残りのお金」のどれにも含まれません。** 生活費とは別枠の記録です。
- 貯金の合計には切り崩しが含まれません。一方、先月までの残高では切り崩しを差し引いた正味の貯金額を使います。
- 「先月からコピー」は重複を確認しません。2回押すと同じ固定費が2件ずつ登録されます。
- 金額は端末の地域設定の通貨形式で表示されます。
