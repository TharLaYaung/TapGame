using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// 固定化されたUIによる視認性低下を防ぎ、プレイヤーの操作を直感的に誘導するためのアニメーション制御
/// ホバーやクリック時のインタラクティブなフィードバックも統合して管理する
public class UIAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
{
    // スケール変更（脈動）アニメーションを有効にするか
    public bool PulseScale = true;
    // 脈動アニメーションの速度
    public float PulseSpeed = 2f;
    // 脈動アニメーションによるスケールの変動幅
    public float PulseAmount = 0.05f;

    // Y軸方向の浮遊（上下移動）アニメーションを有効にするか
    public bool FloatPosition = false;
    // 浮遊アニメーションの速度
    public float FloatSpeed = 1.5f;
    // 浮遊アニメーションの移動幅（ピクセルなど）
    public float FloatAmount = 10f;

    // 回転アニメーションを有効にするか
    public bool Rotate = false;
    // 回転アニメーションの1秒あたりの角度
    public float RotateSpeed = 30f;

    // 色のフェード（明滅）アニメーションを有効にするか
    public bool ColorFade = false;
    // 色のフェードアニメーションの速度
    public float ColorFadeSpeed = 2f;
    // フェードアニメーションで変化する目標の色
    public Color TargetColor = Color.white;

    // 滑らかに全色相を遷移するレインボーカラーを有効にするか
    public bool RainbowColor = false;
    public float RainbowSpeed = 0.5f;

    // 各アニメーション計算の基準となるスカラー値
    private const float BaseScale = 1.0f;

    // スケール変更の基準となる初期スケール
    private Vector3 startScale;
    // 浮遊移動の基準となる初期のローカル座標
    private Vector3 startPosition;
    // 色変更の基準となる初期の色
    private Color startColor;
    // 色を変更するためのUIグラフィックコンポーネントへの参照
    private Graphic targetGraphic;

    // --- インタラクション用の状態変数 ---
    private bool isHovered = false;
    private bool isPressed = false;
    private float currentInteractionScale = 1.0f; // 現在のインタラクション倍率

    // イベントハンドラーの実装（RaycastTargetが有効なUIのみ反応する）
    public void OnPointerEnter(PointerEventData eventData) { isHovered = true; }
    public void OnPointerExit(PointerEventData eventData) { isHovered = false; isPressed = false; }
    public void OnPointerDown(PointerEventData eventData) { isPressed = true; }
    public void OnPointerUp(PointerEventData eventData) { isPressed = false; }
    public void OnSelect(BaseEventData eventData) { isHovered = true; }
    public void OnDeselect(BaseEventData eventData) { isHovered = false; isPressed = false; }

    private void Start()
    {
        // 初期状態のスケールを保存
        startScale = transform.localScale;
        // 初期状態のローカル座標を保存
        startPosition = transform.localPosition;

        // ImageやTextなど、色を持つUIコンポーネントを取得
        targetGraphic = GetComponent<Graphic>();
        if (targetGraphic != null)
        {
            // 初期状態の色を保存
            startColor = targetGraphic.color;
        }
    }

    private void Update()
    {
        // インタラクション（ホバー・クリック）に応じた目標スケールを決定
        // 押下時は少し凹み(0.95倍)、ホバー/選択時は少し拡大(1.15倍)、通常時は等倍(1.0倍)
        float targetInteractionScale = isPressed ? 0.95f : (isHovered ? 1.15f : 1.0f);
        // 目標スケールに向けて滑らかに補間（弾力のある動き）
        currentInteractionScale = Mathf.Lerp(currentInteractionScale, targetInteractionScale, Time.deltaTime * 15f);

        // 脈動アニメーションが有効な場合
        if (PulseScale)
        {
            // サイン波を使って一定のリズムで変動する倍率を計算
            float scaleModifier = BaseScale + Mathf.Sin(Time.time * PulseSpeed) * PulseAmount;
            // 脈動の倍率とインタラクションの倍率を掛け合わせて適用
            transform.localScale = startScale * scaleModifier * currentInteractionScale;
        }
        else
        {
            // 脈動が無効な場合でも、ホバーやクリックのフィードバックは適用する
            transform.localScale = startScale * currentInteractionScale;
        }

        // 浮遊アニメーションが有効な場合
        if (FloatPosition)
        {
            // サイン波を使ってY軸方向の変位を計算
            float yModifier = Mathf.Sin(Time.time * FloatSpeed) * FloatAmount;
            // 初期位置にY軸の変位を足し合わせる
            transform.localPosition = startPosition + new Vector3(0, yModifier, 0);
        }

        // 回転アニメーションが有効な場合
        if (Rotate)
        {
            // Z軸を中心にフレームの経過時間分だけ回転させる
            transform.Rotate(0, 0, RotateSpeed * Time.deltaTime);
        }

        // レインボーアニメーションが有効な場合
        if (RainbowColor && targetGraphic != null)
        {
            // 時間経過で色相(Hue)を滑らかに1周させる
            targetGraphic.color = Color.HSVToRGB((Time.time * RainbowSpeed) % 1f, 0.8f, 1f);
        }
        // 色フェードアニメーションが有効かつ、対象のUIコンポーネントが存在する場合
        else if (ColorFade && targetGraphic != null)
        {
            // サイン波の-1〜1の範囲を0〜1に正規化してフェード進行度を計算（より滑らかにするためSinを利用）
            float fadeProgress = (Mathf.Sin(Time.time * ColorFadeSpeed) + 1f) * 0.5f;
            // 初期色と目標色を進行度に応じてブレンドする
            targetGraphic.color = Color.Lerp(startColor, TargetColor, fadeProgress);
        }
    }
}
