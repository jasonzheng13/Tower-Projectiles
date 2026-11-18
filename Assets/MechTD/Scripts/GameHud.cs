using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace MechTD
{
    /// <summary>
    /// Screen overlay: status, the "how to deploy" instructions, mech selection cards,
    /// placement hints and the end-of-game screen. Built entirely from code.
    /// </summary>
    public class GameHud : MonoBehaviour
    {
        private Font font;
        private Text statusText, bannerText, hintText, endTitle, endBody;
        private GameObject endPanel;
        private readonly Image[] cardImages = new Image[3];
        private readonly Text[] cardTexts = new Text[3];
        private TowerPlacer placer;

        private static readonly Color PanelColor = new Color(0.04f, 0.06f, 0.1f, 0.82f);
        private static readonly Color Cyan = new Color(0.45f, 0.9f, 1f);

        void Start()
        {
            placer = GetComponent<TowerPlacer>();
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                es.transform.SetParent(transform, false);
            }

            var canvasGo = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            RectTransform root = canvasGo.GetComponent<RectTransform>();

            // Title + status (top-left)
            RectTransform status = Panel(root, new Vector2(0, 1), new Vector2(20, -20), new Vector2(430, 215));
            Label(status, "STEEL VANGUARD", 30, FontStyle.Bold, Cyan, new Vector2(18, -12), new Vector2(400, 40));
            Label(status, "Federation Defense Line  //  Colony War", 16, FontStyle.Italic, new Color(0.7f, 0.75f, 0.8f), new Vector2(18, -48), new Vector2(400, 24));
            statusText = Label(status, "", 21, FontStyle.Normal, Color.white, new Vector2(18, -78), new Vector2(400, 130));

            // How to deploy (top-right) - required instructions overlay
            RectTransform help = Panel(root, new Vector2(1, 1), new Vector2(-20, -20), new Vector2(470, 330));
            Label(help, "HOW TO DEPLOY MECHS", 24, FontStyle.Bold, new Color(1f, 0.8f, 0.3f), new Vector2(18, -12), new Vector2(440, 34));
            Label(help,
                "1.  Press <b>1</b>, <b>2</b> or <b>3</b> (or click a card below) to pick a mech.\n" +
                "2.  Move the mouse over open ground. The preview turns <color=#55ff77>GREEN</color> where you can deploy, <color=#ff5555>RED</color> where you can't.\n" +
                "3.  <b>LEFT-CLICK</b> to deploy. You can't build on the enemy route.\n" +
                "4.  <b>RIGHT-CLICK</b> or <b>ESC</b> cancels.\n\n" +
                "Deployments are limited. Clearing a wave adds +1.\n" +
                "Mechs fire automatically and lead moving targets.\n" +
                "<color=#ff7755>If any enemy reaches the base, you lose.</color>",
                18, FontStyle.Normal, Color.white, new Vector2(18, -52), new Vector2(440, 270));

            // Controls (bottom-right)
            RectTransform controls = Panel(root, new Vector2(1, 0), new Vector2(-20, 20), new Vector2(330, 128));
            Label(controls,
                "<b>CAMERA</b>  WASD pan  |  Q/E rotate\n             Scroll zoom\n" +
                "<b>SPACE</b>  launch next wave now\n<b>F</b>  toggle 2x speed   <b>R</b>  restart (after game)",
                16, FontStyle.Normal, new Color(0.8f, 0.85f, 0.9f), new Vector2(14, -12), new Vector2(310, 110));

            // Mech cards (bottom-center)
            for (int i = 0; i < 3; i++) BuildCard(root, i);

            // Banner (top-center) and placement hint (above cards)
            bannerText = Label(root, "", 34, FontStyle.Bold, new Color(1f, 0.85f, 0.35f), Vector2.zero, new Vector2(1000, 50), new Vector2(0.5f, 1));
            bannerText.alignment = TextAnchor.MiddleCenter;
            bannerText.rectTransform.anchoredPosition = new Vector2(0, -40);
            hintText = Label(root, "", 22, FontStyle.Bold, Color.white, Vector2.zero, new Vector2(1100, 36), new Vector2(0.5f, 0));
            hintText.alignment = TextAnchor.MiddleCenter;
            hintText.rectTransform.anchoredPosition = new Vector2(0, 225);

            // End-of-game screen
            endPanel = new GameObject("EndScreen", typeof(RectTransform), typeof(Image));
            endPanel.transform.SetParent(root, false);
            var endRt = endPanel.GetComponent<RectTransform>();
            endRt.anchorMin = Vector2.zero; endRt.anchorMax = Vector2.one;
            endRt.offsetMin = endRt.offsetMax = Vector2.zero;
            endPanel.GetComponent<Image>().color = new Color(0, 0, 0, 0.7f);
            endTitle = Label(endRt, "", 80, FontStyle.Bold, Color.white, Vector2.zero, new Vector2(1400, 110), new Vector2(0.5f, 0.5f));
            endTitle.alignment = TextAnchor.MiddleCenter;
            endTitle.rectTransform.anchoredPosition = new Vector2(0, 80);
            endBody = Label(endRt, "", 28, FontStyle.Normal, Color.white, Vector2.zero, new Vector2(1400, 160), new Vector2(0.5f, 0.5f));
            endBody.alignment = TextAnchor.UpperCenter;
            endBody.rectTransform.anchoredPosition = new Vector2(0, -40);
            endPanel.SetActive(false);
        }

        private void BuildCard(RectTransform root, int index)
        {
            TowerStats s = TowerStats.All[index];
            var go = new GameObject("Card " + s.Name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(root, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0);
            rt.sizeDelta = new Vector2(300, 190);
            rt.anchoredPosition = new Vector2((index - 1) * 315, 20);
            var img = go.GetComponent<Image>();
            img.color = PanelColor;
            cardImages[index] = img;
            TowerKind kind = s.Kind;
            go.GetComponent<Button>().onClick.AddListener(() => placer.Select(kind));

            // Color stripe
            var stripe = new GameObject("Stripe", typeof(RectTransform), typeof(Image));
            stripe.transform.SetParent(rt, false);
            var srt = stripe.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0, 1); srt.anchorMax = new Vector2(1, 1); srt.pivot = new Vector2(0.5f, 1);
            srt.sizeDelta = new Vector2(0, 6);
            stripe.GetComponent<Image>().color = s.ShotColor;
            stripe.GetComponent<Image>().raycastTarget = false;

            cardTexts[index] = Label(rt,
                $"<size=24><b>[{index + 1}] {s.Name}</b></size>\n<i>{s.Role}</i>\n" +
                $"Fire rate      {s.FireRate:0.0} shots/s\n" +
                $"Shell speed  {s.ProjectileSpeed:0} m/s\n" +
                $"Height         {s.Height:0.#} m\n" +
                $"Range  {s.Range:0} m   Damage  {s.Damage:0}{(s.SplashRadius > 0 ? " (splash)" : "")}",
                17, FontStyle.Normal, Color.white, new Vector2(14, -12), new Vector2(280, 175));
        }

        void Update()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || statusText == null) return;

            string wave = gm.WaveNumber == 0 ? "-" : gm.WaveNumber.ToString();
            statusText.text =
                $"Wave  <b>{wave} / {gm.TotalWaves}</b>\n" +
                $"Enemies on field  <b>{gm.Enemies.Count}</b>\n" +
                $"Score  <b>{gm.Score:#,0}</b>     Kills  <b>{gm.Kills}</b>\n" +
                $"Mechs deployed  <b><color=#7fe7ff>{gm.Towers.Count} / {gm.TowerLimit}</color></b>  ({gm.TowersRemaining} left)" +
                (gm.FastForward ? "\n<color=#ffcc55>>> 2x SPEED</color>" : "");

            if (!string.IsNullOrEmpty(gm.Message)) bannerText.text = gm.Message;
            else if (gm.State == GameState.Playing && !gm.WaveInProgress && gm.Countdown > 0f)
                bannerText.text = $"Wave {gm.WaveNumber + 1} arrives in {Mathf.CeilToInt(gm.Countdown)}s  -  deploy your mechs  (SPACE = launch now)";
            else bannerText.text = "";

            hintText.text = placer.Hint;
            hintText.color = placer.HintIsError ? new Color(1f, 0.45f, 0.4f) : new Color(0.6f, 1f, 0.65f);

            for (int i = 0; i < 3; i++)
            {
                bool selected = placer.Selected.HasValue && (int)placer.Selected.Value == i;
                cardImages[i].color = selected ? new Color(0.12f, 0.3f, 0.45f, 0.95f) : PanelColor;
                cardTexts[i].color = gm.TowersRemaining > 0 ? Color.white : new Color(1f, 1f, 1f, 0.45f);
            }

            if (gm.State != GameState.Playing && !endPanel.activeSelf)
            {
                endPanel.SetActive(true);
                bool won = gm.State == GameState.Victory;
                endTitle.text = won ? "VICTORY" : "BASE OVERRUN";
                endTitle.color = won ? new Color(0.5f, 1f, 0.6f) : new Color(1f, 0.35f, 0.3f);
                endBody.text = (won ? $"All {gm.TotalWaves} waves repelled. The colony fleet is in retreat." : gm.DefeatReason) +
                               $"\n\nScore {gm.Score:#,0}   |   Kills {gm.Kills}   |   Wave {gm.WaveNumber}/{gm.TotalWaves}\n\n<b>Press R to play again</b>";
            }
        }

        // ---------- UI helpers ----------

        private RectTransform Panel(RectTransform parent, Vector2 corner, Vector2 offset, Vector2 size)
        {
            var go = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = corner;
            rt.anchoredPosition = offset;
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.color = PanelColor;
            img.raycastTarget = false;
            return rt;
        }

        private Text Label(RectTransform parent, string text, int size, FontStyle style, Color color,
                           Vector2 topLeftOffset, Vector2 box, Vector2? anchor = null)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(Shadow));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            Vector2 a = anchor ?? new Vector2(0, 1);
            rt.anchorMin = rt.anchorMax = a;
            rt.pivot = anchor.HasValue ? a : new Vector2(0, 1);
            rt.anchoredPosition = topLeftOffset;
            rt.sizeDelta = box;
            var t = go.GetComponent<Text>();
            t.font = font;
            t.text = text;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = color;
            t.supportRichText = true;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            go.GetComponent<Shadow>().effectDistance = new Vector2(1.5f, -1.5f);
            return t;
        }
    }
}
