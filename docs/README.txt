Dynamic Bone Collider Assignment Fix 0.2.1
コイカツ（KK）／コイカツサンシャイン（KKS）用

■ インストール・更新
Studio を終了し、自分のゲームに合う DLL を BepInEx\plugins に配置してください。
コイカツ: KK_DynamicBoneColliderAssignmentFix.dll
サンシャイン: KKS_DynamicBoneColliderAssignmentFix.dll
ゲームに合う1 DLLだけを導入してください。
旧0.1.0／0.2.0から更新する場合は同名のDLLを置き換えます。
別フォルダに旧版の同じプラグインが残らないようにしてください。
ゲーム本体やほかのプラグインのDLLを上書きする必要はありません。

■ 基本動作
通常操作不要。Studio 起動時に動作します。
設定済みの独立した Collider について、KKPE/KKSPE が既存キャラの骨を再認識する
際に、新しい Dynamic Bone の自動 ON を防ぎます。既存の登録済み ON/OFF と、
生存している既存対象の適用状態を維持します。
衣装変更等も同じ再認識処理を通る場合は対象になります。

置換で破棄された骨と新しい骨を名前で対応付ける機能はありません。
以前 ON の髪でも再生成された場合は OFF になります。必要な新しい骨は
KKPE/KKSPE で手動 ON にしてください。
新規キャラの初期生成、新規 Collider の初期適用、キャラ内蔵 Collider は
一律に変更しません。独自のシーン保存データも追加しません。
「Enable New Dynamic Bones」は再認識中だけ一時的に OFF として扱い、元へ戻します。

■ 将来の更新への対応
HF Patch のバージョン番号や PoseEditor の DLL 名では判定しません。
導入済み PoseEditor の GUID、内部データの型、認識処理、Harmony API を検査します。
内部の名前が変わっても、構造を一意に確認できる範囲では自動的に対応します。
大きな仕様変更や曖昧な構造では保護を停止し、理由をログへ記録します。
将来の全バージョンでの動作保証ではありません。
現在の割当辞書と新規対象の既定設定が存在しない旧 PoseEditor は非対応です。

■ 設定
BepInEx Configuration Manager の「Dynamic Bone Collider Assignment Fix」から設定。
PreserveColliderAssignments: true（既定）。false で保護を停止。
DebugLogging: false（既定）。true で再認識時の診断ログを出力。
設定ファイル:
KK: BepInEx\config\io.github.KCFunica.kk.dynamicbonecolliderassignmentfix.cfg
KKS: BepInEx\config\io.github.KCFunica.kks.dynamicbonecolliderassignmentfix.cfg
公開用GUIDの変更に伴い、旧設定が同じconfigフォルダに1つだけ存在する場合は新しい設定ファイルへコピーして読み込みます。新しい設定が既にある場合は上書きしません。候補が複数なら自動移行をせずログに記録します。旧ファイルは削除しません。
無効化しても、既に OFF にした対象を自動 ON には戻しません。

■ アンインストール
Studio を終了し、追加した DLL を削除してください。
OFFとして保存された対象は、KKPE/KKSPEの通常の割当として残ります。

■ 検証状況
0.2.1は KK版／KKS版とも33件の自動テストに成功しています。
0.2.0で古いUnity Mono上のHarmony引数変換に失敗して保護が停止する問題を修正しました。
KKPE 2.21.4、KKSPE 2.21.5への接続、生成DLLのUnity参照も検証済みです。
KK版0.2.1は利用者によるキャラ入れ替え後の動作確認済みです。KKS版は実機未確認です。
KKS版はサンシャイン本体の参照DLLと公式KKSPEで検証しており、HF導入済みの
KKS Studioでの起動・操作はまだ確認していません。

■ 実機での確認
元シーンを残し、結果は別名で保存してください。
1. DBCを使うシーンで、一部の骨だけONにしてキャラを置換。
2. 新キャラの髪・服等が勝手にONにならず、既存アイテムへの作用が残るか確認。
3. 複数Collider、Inside用途でも確認。
4. 別名保存、Studio再起動、再読込後にもキャラ置換を確認。

■ トラブル時
DebugLogging=trueにして再現し、BepInEx\LogOutput.logを確認してください。
ファイルがない環境ではゲームフォルダのoutput_log.txtを確認してください。
「Plugin loaded」は起動時の検査とPatch登録成功、
「Assignment protection stopped」は保護が停止した状態です。
ログには検出したPoseEditorのAssembly名・版、認識メソッド、HarmonyのAPIを記録します。

■ ライセンス・作者
MIT License。Copyright (c) 2026 KCFunica
作者: KCFunica / 公開用識別名: KCFunica
LICENSEとTHIRD_PARTY_NOTICES.mdを参照してください。
今回の公開準備ではゲームを起動していません。0.2.1の保存・再読込は個別確認済みとは扱いません。


公開用GUIDの変更と旧設定の自動引き継ぎを追加した今回のDLLは実機未確認です。上記の利用者確認は変更前の0.2.1についての記録です。割当保持の処理自体は変更していません。
