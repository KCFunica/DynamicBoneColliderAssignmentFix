# 公開用に変更した箇所

2026-10-14作成。GitHubへの送信は実施していません。

| ファイル | 変更内容 |
| --- | --- |
| LICENSE | 利用者承認済みのMIT License全文。著作権表示は `Copyright (c) 2026 KCFunica`。 |
| README.md | 日本語の導入・依存関係・ビルド・配布手順。確認済み環境と未確認環境を区別。作者unica／公開用識別名KCFunica。 |
| src/Plugin.cs | AssemblyCompanyはunica、AssemblyCopyrightは指定どおり。KK/KKSのGUIDを `io.github.KCFunica...` へ変更。旧設定を安全にコピーして再読込。 |
| src/ConfigMigration.cs | 設定引き継ぎ処理を追加。新設定が存在すれば上書きしない。同じゲーム向けの旧設定が1つだけならコピーし、旧ファイルは残す。候補が複数なら移行を止めてログに記録。 |
| src/*.cs・tests/RegressionTests.cs | 本プロジェクトの著作権コメントをunicaに統一。DLLの公開GUID・著作権メタデータ検査と4件の移行テストを追加。 |
| docs/README.txt | 新しい設定ファイル名と移行条件、MIT、作者名、実機未確認範囲を記載。 |
| build.ps1 | Release構成を明示し、最適化有効・デバッグ情報なしでビルド。参照DLLをゲームへコピーしない。 |
| KK_DynamicBoneColliderAssignmentFix.csproj | Releaseを既定にし、重複設定を整理。設定移行ソースをコンパイル対象へ追加。 |
| .gitignore・.gitattributes | バイナリ、調査資料、ゲームデータ、個人設定、鍵、ログを除外。ソースの改行規則を設定。 |
| tools/Package-Release.ps1 | インストール先を示すZIP構成、LICENSEと説明の同梱、全エントリのSHA256照合を追加。 |
| THIRD_PARTY_NOTICES.md | 依存関係と調査資料の扱い。外部著作権表示と許諾文を保持。 |
| docs/RELEASE_NOTES_0.2.1.md | GitHub Releases用の日本語リリースノート。公開用名称変更・設定移行・実機未確認を明記。 |
| docs/LICENSE_CHOICES.md | MIT／Apache-2.0の候補提示と、MIT採用の承認結果を記録。 |
| ローカルGit設定（公開対象外） | このリポジトリだけ `user.name=unica` を設定。本人から提示されたGitHub noreplyメールをこのリポジトリだけに設定。仮アドレスと個人メールは設定していない。 |

Dynamic Bone Colliderの割当保持アルゴリズム、既存ON/OFFの扱い、保存形式、DLLファイル名とバージョン0.2.1は変更していません。公開用GUIDの変更で設定ファイル名とHarmonyの所有者IDは変わります。旧DLLは同じファイル名で置き換え、二重に導入しないでください。

既存の開発フォルダにGit履歴はありませんでした。公開用フォルダには新規ローカルリポジトリを作成しました。既存履歴の書き換え・ゲーム本体の変更・調査資料の削除は行っていません。
