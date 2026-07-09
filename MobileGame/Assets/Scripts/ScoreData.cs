
/// ゲームシーンからリザルトシーンへスコアを引き継ぐためのデータコンテナ
/// MonoBehaviourを介さないことでシーンロード時の破棄を回避
public static class ScoreData
{
    // ゲームオーバー時にゲームシーンから保存され、リザルト画面で表示される最終スコア
    public static int FinalScore = 0;
}
