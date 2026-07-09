using UnityEngine;
using UnityEngine.UI;

/// PC向けエイムゲーム用入力システム。
/// OSのカーソルを非表示にして画面内にロックし、マウスの移動量から独自の照準（クロスヘア）を制御する。
/// クリック時に照準位置からのRaycastで的を破壊する。
public class AimController : MonoBehaviour
{
    // 画面上に描画する照準のUI画像
    private Image crosshairImage;
    // 照準の論理的なスクリーン座標
    private Vector2 simulatedMousePosition;
    // マウス入力にかける基本感度倍率（OSの差異を吸収）
    private float baseSensitivity = 15f;

    private void Start()
    {
        // タイトルを経由せずにゲームシーンから直接デバッグ起動した場合などのため、ここでも感度を読み込む
        GameSettings.MouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 1.0f);

        // OS標準のマウスカーソルを非表示にし、画面内にロックする
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 初期位置を画面中央に設定
        simulatedMousePosition = new Vector2(Screen.width / 2f, Screen.height / 2f);

        // 照準UIを動的生成する
        CreateCrosshairUI();
    }

    private void CreateCrosshairUI()
    {
        // 照準を配置するためのCanvasを生成
        GameObject canvasObj = new GameObject("AimCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // 最前面に表示

        // UIスケール調整用のコンポーネントを追加
        UnityEngine.UI.CanvasScaler scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);

        // 照準となる画像オブジェクトを生成
        GameObject crosshairObj = new GameObject("Crosshair");
        crosshairObj.transform.SetParent(canvasObj.transform, false);
        crosshairImage = crosshairObj.AddComponent<Image>();
        
        // 単純な白い円として描画（画像リソース不要な方法）
        // クロスヘア（十字）を作るための子要素を作成（サイズを大きくし視認性を向上）
        CreateCrosshairLine(crosshairObj, new Vector2(4, 40), Vector2.zero); // 縦線
        CreateCrosshairLine(crosshairObj, new Vector2(40, 4), Vector2.zero); // 横線
        // 中央のドット
        CreateCrosshairLine(crosshairObj, new Vector2(8, 8), Vector2.zero);

        // 親のImageコンポーネントは透明にする
        crosshairImage.color = new Color(0, 0, 0, 0);
    }

    private void CreateCrosshairLine(GameObject parent, Vector2 size, Vector2 pos)
    {
        GameObject line = new GameObject("Line");
        line.transform.SetParent(parent.transform, false);
        Image img = line.AddComponent<Image>();
        // 背景や他のエフェクトに埋もれないよう、最も明るい白を採用
        img.color = Color.white;
        RectTransform rect = line.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = pos;

        // 視認性向上のために全方位を囲むアウトライン（黒縁）を追加
        UnityEngine.UI.Outline outline = line.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2, -2);
    }

    private void Update()
    {
        // ゲームオーバー時は操作を受け付けない
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
        {
            if (crosshairImage != null && crosshairImage.gameObject.activeSelf)
                crosshairImage.gameObject.SetActive(false);
            return;
        }

        // ポーズ中はクロスヘアを消して処理をスキップ（カーソルロックはUIManager側で解除済み）
        if (GameManager.Instance != null && GameManager.Instance.IsPaused)
        {
            if (crosshairImage != null && crosshairImage.gameObject.activeSelf)
                crosshairImage.gameObject.SetActive(false);
            return;
        }

        // ポーズから復帰した時などに確実にカーソルをロックしてクロスヘアを表示する
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (crosshairImage != null && !crosshairImage.gameObject.activeSelf)
        {
            crosshairImage.gameObject.SetActive(true);
        }

        // マウスの移動量（Delta）を取得
        float mouseX = Input.GetAxisRaw("Mouse X");
        float mouseY = Input.GetAxisRaw("Mouse Y");

        // 設定された感度を乗算して論理位置を更新
        float currentSensitivity = baseSensitivity * GameSettings.MouseSensitivity;
        simulatedMousePosition.x += mouseX * currentSensitivity;
        simulatedMousePosition.y += mouseY * currentSensitivity;

        // 画面外に出ないようにクランプ
        simulatedMousePosition.x = Mathf.Clamp(simulatedMousePosition.x, 0, Screen.width);
        simulatedMousePosition.y = Mathf.Clamp(simulatedMousePosition.y, 0, Screen.height);

        // UI画像の座標を更新
        if (crosshairImage != null)
        {
            crosshairImage.rectTransform.position = simulatedMousePosition;
        }

        // クリック入力（またはスペースキー等）の判定
        if (Input.GetMouseButtonDown(0))
        {
            FireRaycast();
        }
    }

    private void FireRaycast()
    {
        if (Camera.main == null) return;

        // 独自の照準位置からRayを飛ばす
        Ray ray = Camera.main.ScreenPointToRay(simulatedMousePosition);
        RaycastHit hit;

        // RayがColliderを持つオブジェクトに当たったか判定
        if (Physics.Raycast(ray, out hit))
        {
            // 当たったオブジェクトがShapeTargetコンポーネントを持っていれば破壊
            ShapeTarget target = hit.collider.GetComponent<ShapeTarget>();
            if (target != null)
            {
                // ボムや強制判定用のパブリックメソッドを呼び出して破壊処理を実行
                target.強制ヒット();
            }
        }
    }
}
