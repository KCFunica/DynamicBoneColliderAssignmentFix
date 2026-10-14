# 第三者ソフトウェア・資料について

本リポジトリと配布ZIPには、ゲーム本体、ゲーム由来DLL、第三者DLL、MOD本体、取得した上流ソース、逆アセンブル結果を含めません。プラグインDLLへの埋め込み・DLL結合も行っていません。依存ファイルは利用者自身の導入環境から参照します。

| 対象 | 用途・同梱状況 | 確認した情報・参照先 |
| --- | --- | --- |
| BepInEx 5.4.23.5 | コンパイル・実行依存。非同梱 | MIT、Copyright (c) 2018 Bepis。[公式LICENSE](https://github.com/BepInEx/BepInEx/blob/v5.4.23.5/LICENSE) |
| HarmonyX（0Harmony 2.9.0.0） | パッチ実行・テスト依存。非同梱 | HarmonyX公式LICENSEはMIT、Copyright (c) 2020 BepInEx。[公式LICENSE](https://github.com/BepInEx/HarmonyX/blob/master/LICENSE)。本資料は導入済みcore内の全依存ライセンスを監査したものではありません。 |
| HSPlugins / KKPE / KKSPE | 実行時に既存APIと内部構造へ接続。非同梱 | [公式HSPlugins](https://github.com/IllusionMods/HSPlugins)、[調査したr2.25配布](https://github.com/IllusionMods/HSPlugins/releases/tag/r2.25)。ローカル保管のr2.25配布LICENSEはMIT、Copyright (c) 2021 DeathWeasel1337。 |
| Mono.Cecil | ビルド後のメタデータ検査のみ。非同梱 | [公式プロジェクト](https://github.com/jbevain/cecil)。導入環境のBepInEx coreから参照します。プラグイン実行時依存ではありません。 |
| Unity・コイカツ／サンシャイン | 正規に取得した環境の参照・実行依存。非同梱 | 本プロジェクトのライセンスはゲームやUnityの再配布を許諾しません。 |

公開対象の `src` は割当保持・反射接続・Harmony呼び出し・互換性検査の実装です。調査用上流ソースのファイルを公開対象へコピーしていません。元の開発フォルダにある調査資料は公開用フォルダの外に保管しています。

調査で参照したHSPluginsのコミットは `9220f2a6b2e6f8e0f62ea9acec6238f5ab6ed1cf` です。配布DLLとmainブランチを同一のものとは扱っていません。

## 調査で確認したHSPlugins r2.25の許諾文

参照元の表示を保持するため、配布アーカイブ内LICENSEの文面を以下に記録します。これは本プラグイン自身のライセンス選択とは別です。

```text
MIT License

Copyright (c) 2021 DeathWeasel1337

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

公開対象を今回確認した範囲では、第三者DLL・ゲームデータの混入や、第三者ソースファイルの直接同梱は見つかっていません。調査資料や参照DLLを今後追加する場合は、個々の再配布条件を再確認してください。
