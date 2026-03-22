# Copilot Instructions

## Project Guidelines
- User prefers uppercase acronym sequences (e.g., XML in XMLHttp) to remain unsplit by the current UpperCaseRule and plans to add dictionary-based protected words later.
- このソリューションおよびプロジェクトでは、回答を常に日本語で行う。
- CaseRules を SymbolNaming へ改名し、責務分離アーキテクチャ（Tokenization/Analysis/Conversion/Dictionaries/Engine）で拡張する。
- UpperCaseRule では digit 判定を行わず、数字境界は PostDigitRule に責務を寄せる。
- ハイフン区切りルールは不要。
- 『既存のものを破壊しない』はコア機能の安定性を指し、publicインターフェイス破壊自体は許容。Tokenは高頻度利用のため小さく軽量に保ち、_inlineTextのような不要メンバは持たせない。
- ユーザーはConversion APIの設計で、単純なstring戻り値より専用Result型やTokenList戻り値のような将来拡張しやすい設計を好む。

## Prefix Handling
- プレフィックス判定は、KnownPrefixesのような生配列ではなく、責務分離のためIPrefixProviderの専用インターフェイスで扱う。
- プレフィックス判定は PrefixProvider（または TokenCategory.Prefix）に限定する。Case分類では ProtectedWordProvider で辞書登録された単語はプレフィックス扱いしない。

## このスレッドで確定した方針
- ライブラリ名・プロジェクト名・アセンブリ名は `SymbolNaming` 系に統一する。
- 責務分離を前提として、`Tokenization` / `Analysis` / `Conversion` / `Dictionaries` / `Engine` に分割する。
- `CaseRule` への機能集約は避け、分割・判定・変換はそれぞれ独立したインターフェイスで拡張可能にする。
- 未確定仕様の領域はスタブ実装を許容し、確定的な処理（既存トークナイズ動作）は本実装として維持する。
