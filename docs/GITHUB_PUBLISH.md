# GitHub Desktopでの初回公開

公開対象はこのファイルがあるリポジトリのルートです。元の開発フォルダ全体を公開しないでください。ソースとDLLの配布ZIPは分けて準備しています。

## 公開前

1. 本人から提示されたnoreplyアドレスと作者名KCFunicaは設定済みです。初回ローカルコミットも作成済みです。GitHub Desktopは同じアカウントでログインしてください。
2. [変更一覧](PUBLICATION_CHANGES.md)、[検査結果](PUBLICATION_AUDIT.md)、[リリースノート](RELEASE_NOTES_0.2.1.md) を確認します。
3. 今回のDLLは名称変更・設定移行追加後のStudio実機確認が未実施です。KKの過去の利用者確認実績と区別してください。KKSの実機未確認表示を維持してください。

## GitHub Desktopでの操作（公開の最終確認後）

1. File → Add local repository → Chooseで公開用フォルダを選択し、Add repositoryを押します。
2. Changesに表示されるのがソース・スクリプト・説明・LICENSEだけであることを確認します。DLL、ZIP、research、ゲームデータはコミットしません。
3. 初回コミットは作成済みです。現在のGit作成者設定はKCFunicaです。過去2件のコミットは変更前の作成者名を保持しています。追加変更がなければコミット操作は不要です。
4. Publish repositoryを押し、名前を `DynamicBoneColliderAssignmentFix` とします。一般公開する時はKeep this code privateのチェックを外し、確認後にPublish repositoryを押します。この操作がGitHubへの公開です。

## 完成DLLの公開（ブラウザでの操作）

1. 公開したリポジトリのReleases → Draft a new releaseを開きます。
2. タグを `v0.2.1`、対象をmain、タイトルを `Dynamic Bone Collider Assignment Fix 0.2.1` とします。
3. RELEASE_NOTES_0.2.1.mdの本文を貼り付けます。実機未確認の表示を残してください。
4. 準備済みのKK版ZIPとSHA256ファイルを添付します。KKS版を添付する場合は `_KKS_unverified.zip` と対応するSHA256ファイルを使います。ソース用の自動生成ZIPには完成DLLは入りません。
5. 内容を確認してPublish releaseを押します。実機確認前の配布として扱う場合はSet as a pre-releaseを選択してください。

参考: [GitHub Desktopへのローカルリポジトリ追加](https://docs.github.com/en/desktop/adding-and-cloning-repositories/adding-a-repository-from-your-local-computer-to-github-desktop)、[既存プロジェクトの公開](https://docs.github.com/en/desktop/adding-and-cloning-repositories/adding-an-existing-project-to-github-using-github-desktop)。
