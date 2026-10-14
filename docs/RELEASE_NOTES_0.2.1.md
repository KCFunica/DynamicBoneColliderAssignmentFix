# Dynamic Bone Collider Assignment Fix 0.2.1

コイカツ／サンシャインのキャラスタジオで、設定済みDynamic Bone Colliderがキャラ置換後の新しい骨へ勝手に適用されることを防ぐ補助プラグインです。初回GitHub公開用にソースと完成DLLの配布構成を整えました。

## ダウンロード

- コイカツ: `DynamicBoneColliderAssignmentFix_v0.2.1_KK.zip`
- サンシャイン: `DynamicBoneColliderAssignmentFix_v0.2.1_KKS_unverified.zip`（Studio実機未確認）

ゲームに合うZIPを1つだけ使用してください。Studio終了後、ZIP内の `BepInEx` フォルダをゲームフォルダへコピーします。旧版は同名DLLを置き換え、別フォルダの重複DLLが残らないよう確認してください。依存DLL・ゲーム本体は含みません。

## 0.2.1の修正

古いUnity MonoでHarmonyの省略可能引数変換に失敗し、保護が停止する問題を修正しました。既定値を明示的に渡すことで対応しています。割当ポリシーや設定ファイルは変更していません。

## 検証と制限

- KK／KKSともReleaseビルド、各33件の自動テスト、生成DLLと実PoseEditorへの接続検査に成功しています。
- KK: HF Patch v3.38、BepInEx 5.4.23.5、Harmony 2.9.0.0、KKPE 2.21.4.0。0.2.1の起動・キャラ入れ替えを2026-10-01に利用者が確認済みです。0.2.1の保存・再読込の個別確認は未実施扱いです。
- KKS: サンシャイン本体の参照DLL、公式KKSPE 2.21.5.0、KK環境のBepInEx coreを参照した自動検査のみです。HF導入済みKKS Studioでの起動・キャラ置換・保存・再読込は未確認です。
- 公開準備時にゲームは起動していません。Windows CLR 4の自動検査はUnity Mono内の操作試験を代替しません。
- 再生成された骨は同名でも新規対象としてOFFになります。必要な骨は手動でONにしてください。
- 旧PoseEditorの `ignoredDynamicBones` 方式、BepInEx 6、将来の全更新、他MODとのあらゆる組合せへの対応は保証しません。

設定・アンインストール・トラブル対処はZIPのREADME.txtをご覧ください。

## ライセンス・公開用表記

MIT License、Copyright (c) 2026 KCFunica。作者名はunica、公開用識別名はKCFunicaです。プラグインGUIDと設定ファイル名を公開用表記へ変更しました。旧設定が1つだけ存在する場合はコピーして引き継ぎ、新しい設定と旧ファイルは上書き・削除しません。候補が複数の場合はログへ記録します。割当保持の処理は変更していません。KKS版は実機未確認です。


公開用GUIDの変更と旧設定の自動引き継ぎを追加した今回のDLLは実機未確認です。上記の利用者確認は変更前の0.2.1についての記録です。割当保持の処理自体は変更していません。
