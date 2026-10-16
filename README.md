# Dynamic Bone Collider Assignment Fix

コイカツ（KK）／コイカツサンシャイン（KKS）のキャラスタジオ用BepInEx 5プラグインです。設定済みの独立したDynamic Bone Colliderについて、キャラ置換で新しく生成された骨が勝手に適用対象になることを防ぎます。現在のバージョンは **0.2.1** です。

## ダウンロードと導入

GitHubで公開後は、このリポジトリの **Releases** からゲームに合うZIPをダウンロードしてください。緑の「Code → Download ZIP」は開発用ソースで、完成DLLは含みません。

| ゲーム | 配布ZIP | 導入するDLL |
| --- | --- | --- |
| コイカツ | `DynamicBoneColliderAssignmentFix_v0.2.1_KK.zip` | `KK_DynamicBoneColliderAssignmentFix.dll` |
| サンシャイン（実機未確認） | `DynamicBoneColliderAssignmentFix_v0.2.1_KKS_unverified.zip` | `KKS_DynamicBoneColliderAssignmentFix.dll` |

1. Studioを終了します。
2. ZIPの中の `BepInEx` フォルダを、ゲームの実行ファイルがあるフォルダへコピーします。
3. `ゲームフォルダ\BepInEx\plugins\対象DLL` という配置になっていることを確認します。
4. 同じプラグインの旧DLLが別フォルダに残っていないことを確認します。ゲームに合う1種類だけを導入してください。

依存MOD・ゲーム本体は同梱していません。アンインストールはStudio終了後に追加したDLLを削除します。詳しい設定・トラブル対処・実機確認手順は [導入説明](docs/README.txt) を参照してください。

## 必要環境と確認範囲

| 項目 | KK | KKS |
| --- | --- | --- |
| 対象 | コイカツ キャラスタジオ | サンシャイン キャラスタジオ |
| 検証参照環境 | HF Patch v3.38、Unity 5.6.2f1 | サンシャイン本体のManaged DLL、Unity 2019系 |
| BepInEx | 5.4.23.5 | 5系を想定。自動検査にはKK環境の5.4.23.5を参照専用で使用 |
| HarmonyX（0Harmony Assembly） | 2.9.0.0 | 自動検査は同じ2.9.0.0 |
| PoseEditor | KKPE 2.21.4.0 | 公式HSPlugins r2.25のKKSPE 2.21.5.0 |
| 自動検証 | Releaseビルド、33件の回帰テスト、実KKPE接続 | Releaseビルド、33件の回帰テスト、実KKSPE接続 |
| Studio実機 | 0.2.1の起動・キャラ入れ替えを利用者が2026-10-01に確認 | **未確認**。HF導入済みKKS Studioで起動・操作していません |

この公開準備ではゲームを起動していません。KK 0.2.1の保存・再読込は個別の確認報告がないため未確認です。過去の0.1.0での保存・再読込実績と区別しています。自動テストはWindows CLR 4上で実施し、Unity Mono内の実機試験を代替しません。

BepInEx Configuration Managerは設定画面を使う場合のみ任意です。BepInEx 6、旧 `ignoredDynamicBones` 方式のPoseEditor、他ゲーム、将来の全HF Patch／PoseEditorに対応済みとはしていません。TimelineやMaterialEditorなどとの個別の共存試験も未実施です。

## 動作

- 既存の登録済みON/OFFと、生存している既存対象への作用を維持します。
- 再生成された骨は以前と同じ名前でも新規対象としてOFFになります。必要な骨はKKPE/KKSPEで手動ONにしてください。
- 新規キャラの初期生成、新規Colliderの初期適用、キャラ内蔵Colliderを一律に変更しません。
- 独自のシーン保存形式は追加しません。OFF状態はPoseEditorの通常の割当として残ります。
- 処理中だけ「Enable New Dynamic Bones」を抑制し、正常終了・例外時とも元の値へ戻します。
- 内部構造が不明・曖昧な環境では保護を停止し、理由をログへ出します。

通常操作は不要です。`PreserveColliderAssignments=true` が既定、`DebugLogging=false` が既定です。無効化しても、既にOFFにした対象を自動ONには戻しません。

## ソースからビルド

WindowsのPowerShellとC#コンパイラーを使用します。Visual StudioやNuGetは不要です。

- KK: Windowsの.NET Framework 3.5機能（`%WINDIR%\Microsoft.NET\Framework\v3.5\csc.exe` が必要）。
- KKS: .NET Framework 4.x（`%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe` が必要）。
- 正規に入手したゲームと、対象のBepInEx 5／PoseEditor環境が必要です。参照DLLは公開リポジトリに含めません。
- `BepInEx\core` に `BepInEx.dll`、`0Harmony.dll`、検査用 `Mono.Cecil.dll` が必要です。回帰テストでHarmonyを実行するため、導入環境の関連core DLLも必要です。
- `CharaStudio_Data\Managed` に `UnityEngine.dll` が必要です。KKSはさらに `UnityEngine.CoreModule.dll` が必要です。検査は `Assembly-CSharp.dll` とその依存DLLも使用するため、Managedフォルダ全体を参照元として残してください。
- `BepInEx\plugins\KKPE.dll` または `KKSPE.dll` と、その依存MODが必要です。PoseEditorは本体コンパイル時には直接参照せず、検査時・実行時に使用します。

PowerShellでソースのフォルダを開き、実際のゲームフォルダを指定します（例の `C:\Games` は利用者環境に合わせて変更）。

```powershell
.\build.ps1 -Configuration Release -Target KK -GameRoot 'C:\Games\Koikatsu'
.\build.ps1 -Configuration Release -Target KKS -GameRoot 'C:\Games\KoikatsuSunshine'
```

実行ポリシーで拒否される場合は、その実行に限って `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -Configuration Release -Target KK -GameRoot 'C:\Games\Koikatsu'` を使用できます。

KKは省略時に `KOIKATSU_ROOT` またはローカルのゲームフォルダを検索します。KKSは `-GameRoot` が必須です。KKSに `CharaStudio_Data\Managed` がない場合は `KoikatsuSunshine_Data\Managed` を参照します。BepInEx coreが見つからない場合、検出したKK環境のcoreを参照専用で使用します。別の参照元は `-CoreRoot`、`-PoseEditorPath` で明示できます。

スクリプトは最適化有効・デバッグ情報なし・警告をエラーとしてコンパイルし、生成DLLの検査と33件の回帰テストを実行します。出力は `Release\KK_DynamicBoneColliderAssignmentFix.dll` と `Release\KKS\KKS_DynamicBoneColliderAssignmentFix.dll` です。ゲームフォルダへの導入・書き込みは行いません。

`csproj` は `Configuration=Release`、`TargetGame=KK/KKS`、`GameRoot`、必要に応じて `CoreRoot`／`ManagedRoot` を指定する従来MSBuild向けです。検査・回帰テストは含まないため、通常は `build.ps1` を使ってください。KKはCLR 2／.NET 3.5、KKSはCLR 4／.NET 4.0向けにコンパイルします。

## 配布ZIPの作成

ビルド成功後、`tools\Package-Release.ps1 -Target KK` または `-Target KKS` を実行すると `dist\0.2.1` にZIPを生成し、内容とSHA256を照合します。LICENSEが存在しない場合は停止します。ZIPにはMIT License全文を同梱します。

## ライセンスと第三者資料

本プラグインは [MIT License](LICENSE) で公開します。著作権表示は `Copyright (c) 2026 KCFunica` です。公開用識別名は `KCFunica`、作者名は `KCFunica` です。

依存ソフトウェア・調査資料の扱いは [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) に記載しています。ゲームDLL・第三者DLL・逆アセンブル資料・ダウンロードしたソース・シーン・キャラカード・ログは公開対象から除外しています。非公式の補助プラグインです。


公開用GUIDの変更と旧設定の自動引き継ぎを追加した今回のDLLは実機未確認です。上記の利用者確認は変更前の0.2.1についての記録です。割当保持の処理自体は変更していません。
