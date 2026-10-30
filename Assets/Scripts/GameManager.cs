using UnityEngine;

[DefaultExecutionOrder(-20)]
public class GameManager : MonoBehaviour
{
    RaceManager race;
    GUIStyle label, number, button;
    Texture2D panel;
    float elapsed;
    bool paused;
    bool selecting = true;
    bool pausedBeforeSelector;
    bool resultSaved;
    bool newBest;
    int finishPlace = 1;

    public bool IsSelecting => selecting;
    public bool IsPaused => paused;

    Color white = TurboArt.Cream;
    Color muted = new Color(0.82f, 0.86f, 0.84f);

    void Start()
    {
        race = FindObjectOfType<RaceManager>();
        OpenSelector();
    }

    void Update()
    {
        if (race == null) race = FindObjectOfType<RaceManager>();
        UpdateTouchInput();
        if (Input.GetKeyDown(KeyCode.M))
        {
            if (selecting) CloseSelector(); else OpenSelector();
            return;
        }
        if (selecting)
        {
            if (Input.GetKeyDown(KeyCode.Escape)) CloseSelector();
            return;
        }
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P)) TogglePause();
        if (race?.player != null && !paused && !race.player.finished)
            elapsed += Time.deltaTime;
    }

    void InitStyles()
    {
        if (label != null) return;
        label = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
        label.normal.textColor = white;
        number = new GUIStyle(label) { fontSize = 31, fontStyle = FontStyle.Normal };
        button = new GUIStyle(GUI.skin.button) { fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        panel = new Texture2D(1, 1);
        panel.SetPixel(0, 0, new Color(0.08f, 0.13f, 0.16f, 0.93f));
        panel.Apply();
        button.normal.background = panel;
        button.hover.background = panel;
        button.active.background = Texture2D.whiteTexture;
        button.normal.textColor = button.hover.textColor = white;
        button.active.textColor = TurboArt.Ink;
        button.border = new RectOffset();
    }

    void OnGUI()
    {
        if (race?.player == null) return;
        InitStyles();
        PlayerController player = race.player;
        TurboCircuit circuit = TurboLevels.Current;

        Matrix4x4 oldMatrix = GUI.matrix;
        Color oldColor = GUI.color;
        float scale = Screen.height / 900f;
        float w = Screen.width / scale;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        Rect safe = Screen.safeArea;
        float left = Mathf.Max(28f, safe.xMin / scale + 16f);
        float right = Mathf.Min(w - 28f, safe.xMax / scale - 16f);
        float top = Mathf.Max(28f, (Screen.height - safe.yMax) / scale + 16f);
        float bottom = Mathf.Min(872f, (Screen.height - safe.yMin) / scale - 16f);

        if (selecting)
        {
            DrawSelector(w, left, right);
            GUI.matrix = oldMatrix;
            GUI.color = oldColor;
            return;
        }

        // Top-left header panel
        DrawPanel(new Rect(left, top, 286, 86));
        Text(new Rect(left + 20, top + 10, 240, 32), "TURBO / X", 25, white);
        Text(new Rect(left + 20, top + 42, 246, 20),
            (TurboLevels.CurrentCircuitIndex + 1).ToString("00") + "  /  " + circuit.Name.ToUpperInvariant(), 11, muted);
        Text(new Rect(left + 20, top + 61, 246, 18),
            "LAP  " + Mathf.Min(player.currentLap, circuit.Laps) + " / " + circuit.Laps + "     ·     POS  " + race.GetPlayerPlace() + " / 4",
            11, TurboArt.Cyan);

        // Top-right action buttons
        if (GUI.Button(new Rect(right - 114, top, 114, 42), paused ? "RESUME" : "PAUSE  II", button)) TogglePause();
        if (GUI.Button(new Rect(right - 114, top + 48, 114, 32), "RETRY  /  R", button)) Restart();
        if (GUI.Button(new Rect(right - 114, top + 86, 114, 32), "CIRCUITS / M", button)) OpenSelector();

        // Stats panel
        float statsX = left;
        float statsY = top + 100;
        DrawPanel(new Rect(statsX, statsY, 286, 116));
        Text(new Rect(statsX + 20, statsY + 8, 120, 18), "SPEED / KM/H", 10, muted);
        GUI.Label(new Rect(statsX + 20, statsY + 26, 115, 45), Mathf.RoundToInt(player.GetSpeedKmh()).ToString("000"), number);
        Text(new Rect(statsX + 148, statsY + 8, 120, 18), "RACE TIME", 10, muted);
        Text(new Rect(statsX + 148, statsY + 27, 120, 38), FormatTime(elapsed), 20, white);

        float shieldFrac = Mathf.Clamp01(player.currentShield / Mathf.Max(1f, player.maxShield));
        Text(new Rect(statsX + 20, statsY + 74, 115, 16), "SHIELD  " + Mathf.RoundToInt(shieldFrac * 100) + "%", 10, muted);
        Fill(new Rect(statsX + 20, statsY + 94, 112, 6), new Color(1, 1, 1, 0.14f));
        Fill(new Rect(statsX + 20, statsY + 94, 112 * shieldFrac, 6), TurboArt.Cyan);

        float turboFrac = Mathf.Clamp01(player.turboEnergy / 100f);
        Text(new Rect(statsX + 148, statsY + 74, 120, 16),
            player.hasWeapon ? "PULSE READY [SPACE]" : "TURBO  " + Mathf.RoundToInt(turboFrac * 100) + "%", 10,
            player.hasWeapon ? TurboArt.Hex(0xA855F7) : TurboArt.Sand);
        Fill(new Rect(statsX + 148, statsY + 94, 118, 6), new Color(1, 1, 1, 0.14f));
        Fill(new Rect(statsX + 148, statsY + 94, 118 * turboFrac, 6), player.hasWeapon ? TurboArt.Hex(0xA855F7) : TurboArt.Accent);

        // Top-center race progress bar
        float progress = Mathf.Clamp01(player.trackDistance / (circuit.TotalLength * circuit.Laps));
        float barWidth = Mathf.Min(340f, right - left - 440f);
        if (barWidth > 120f)
        {
            float barX = (left + right - barWidth) * 0.5f;
            Text(new Rect(barX, top, barWidth, 22), "RACE PROGRESS                     " + Mathf.RoundToInt(progress * 100) + "%", 11, white);
            Fill(new Rect(barX, top + 30, barWidth, 3), new Color(1f, 1f, 1f, 0.22f));
            Fill(new Rect(barX, top + 30, barWidth * progress, 3), TurboArt.Cyan);
            Fill(new Rect(barX + barWidth * progress - 3, top + 27, 6, 9), TurboArt.Accent);
        }

        // Bottom controls
        bool touch = Application.isMobilePlatform || Input.touchSupported;
        if (touch && !paused && !player.finished)
        {
            Rect[] controls = TouchRects();
            TouchButton(controls[0], "STEER\nLEFT", player.touchLeft);
            TouchButton(controls[1], "STEER\nRIGHT", player.touchRight);
            TouchButton(controls[2], player.hasWeapon ? "FIRE\nPULSE" : "BRAKE", player.touchFire || player.touchBrake);
            TouchButton(controls[3], "TURBO\nBOOST", player.touchBoost);
        }
        else
        {
            Text(new Rect(left, bottom - 28, right - left, 28),
                "A D / LEFT RIGHT   STEER      W / SHIFT   TURBO BOOST      S / DOWN   BRAKE      SPACE   FIRE PULSE", 11, white);
        }

        // Pause or Finish Modal
        if ((paused || player.finished) && !selecting)
        {
            Fill(new Rect(0, 0, w, 900), new Color(0.05f, 0.09f, 0.12f, 0.55f));
            float width = Mathf.Min(500f, right - left);
            Rect card = new Rect((w - width) / 2f, 250f, width, 390f);
            DrawPanel(card);
            Fill(new Rect(card.x, card.y, width, 4), TurboArt.Accent);

            string placeStr = finishPlace == 1 ? "1ST PLACE" : finishPlace == 2 ? "2ND PLACE" : finishPlace == 3 ? "3RD PLACE" : "4TH PLACE";
            Text(new Rect(card.x + 32, card.y + 28, width - 64, 22),
                paused ? "RACE PAUSED" : circuit.Name.ToUpperInvariant() + "  ·  " + placeStr, 11, muted);
            Text(new Rect(card.x + 32, card.y + 62, width - 64, 54),
                paused ? "Hold position." : (finishPlace <= 3 ? "Podium finish." : "Race complete."), 34, white);
            Text(new Rect(card.x + 32, card.y + 128, width - 64, 34),
                paused ? "Take a breath, then drop back into the slipstream." : "TIME  " + FormatTime(elapsed) + "     ·     CRAFT  " + TurboLevels.CurrentShip.Name.ToUpperInvariant(),
                12, muted);

            if (player.finished && TurboLevels.BestTime(TurboLevels.CurrentCircuitIndex) > 0f)
            {
                Text(new Rect(card.x + 32, card.y + 168, width - 64, 24),
                    (newBest ? "NEW CIRCUIT RECORD  /  " : "BEST  /  ") + FormatTime(TurboLevels.BestTime(TurboLevels.CurrentCircuitIndex)),
                    12, TurboArt.Sand);
            }

            if (GUI.Button(new Rect(card.x + 32, card.y + 212, width - 64, 50), paused ? "RESUME RACE" : "RACE AGAIN   /   R", button))
            {
                if (paused) TogglePause(); else Restart();
            }
            bool hasNext = player.finished && finishPlace <= 3 && TurboLevels.CurrentCircuitIndex + 1 < TurboLevels.All.Length;
            if (GUI.Button(new Rect(card.x + 32, card.y + 274, width - 64, 44), hasNext ? "NEXT CIRCUIT  >" : "SELECT CIRCUIT & CRAFT", button))
            {
                if (hasNext) StartCircuit(TurboLevels.CurrentCircuitIndex + 1); else OpenSelector();
            }
            if (player.finished)
            {
                Text(new Rect(card.x + 32, card.y + 332, width - 64, 26),
                    finishPlace <= 3 ? "Top-3 finish qualified. Next circuit unlocked." : "Finish in the Top 3 to unlock the next circuit.",
                    12, muted);
            }
        }

        GUI.matrix = oldMatrix;
        GUI.color = oldColor;
    }

    void DrawSelector(float w, float left, float right)
    {
        Fill(new Rect(0, 0, w, 900), new Color(0.05f, 0.09f, 0.12f, 0.78f));
        float width = Mathf.Min(960f, right - left);
        float x = (w - width) / 2f;
        float y = 54f;
        DrawPanel(new Rect(x, y, width, 790f));
        Fill(new Rect(x, y, width, 4f), TurboArt.Accent);

        Text(new Rect(x + 28, y + 18, width - 56, 20), "TURBO / X     —     ANTI-GRAVITY CHAMPIONSHIP", 12, TurboArt.Sand);
        Text(new Rect(x + 28, y + 42, width - 56, 44), "Select craft & circuit.", width < 560 ? 24 : 32, white);

        // Ship Selector Row
        float shipW = (width - 56f - 24f) / 3f;
        for (int s = 0; s < TurboLevels.Ships.Length; s++)
        {
            var ship = TurboLevels.Ships[s];
            float sx = x + 28f + s * (shipW + 12f);
            float sy = y + 96f;
            bool active = s == TurboLevels.CurrentShipIndex;
            Fill(new Rect(sx, sy, shipW, 88f), active ? new Color(0.22f, 0.36f, 0.42f, 0.55f) : new Color(0.16f, 0.22f, 0.26f, 0.32f));
            if (active) Fill(new Rect(sx, sy, shipW, 3f), TurboArt.Hex(ship.Accent));
            Text(new Rect(sx + 14, sy + 8, shipW - 28, 18), ship.Role, 10, TurboArt.Hex(ship.Accent));
            Text(new Rect(sx + 14, sy + 26, shipW - 28, 26), ship.Name, 18, white);
            Text(new Rect(sx + 14, sy + 54, shipW - 28, 22),
                "SPD " + Mathf.RoundToInt(ship.MaxSpeed * 3.6f) + "   ·   SHD " + Mathf.RoundToInt(ship.MaxShield), 10, muted);
            if (GUI.Button(new Rect(sx + shipW - 86, sy + 24, 74, 38), active ? "SELECTED" : "SELECT", button))
            {
                TurboLevels.CurrentShipIndex = s;
                race?.player?.RebuildShipVisual();
            }
        }

        // Circuit Rows
        for (int i = 0; i < TurboLevels.All.Length; i++)
        {
            TurboCircuit c = TurboLevels.All[i];
            float rowY = y + 204f + i * 124f;
            float rowW = width - 56f;
            bool unlocked = TurboLevels.IsUnlocked(i);
            Fill(new Rect(x + 28, rowY, rowW, 112f), new Color(0.22f, 0.31f, 0.35f, unlocked ? 0.28f : 0.11f));
            Text(new Rect(x + 44, rowY + 10, rowW - 140, 18),
                (i + 1).ToString("00") + "  /  " + c.Tag + "  /  " + Mathf.RoundToInt(c.TotalLength) + " M  ×  " + c.Laps + " LAPS",
                10, muted);
            Text(new Rect(x + 44, rowY + 32, rowW - 140, 32), c.Name, width < 560 ? 18 : 23, unlocked ? white : muted);
            float best = TurboLevels.BestTime(i);
            string detail = !unlocked
                ? "Finish Top 3 on " + TurboLevels.All[i - 1].Name + " to unlock"
                : best > 0f ? "BEST  " + FormatTime(best) + "   ·   " + c.Description : c.Description;
            Text(new Rect(x + 44, rowY + 68, rowW - 150, 26), detail, 11, muted);

            bool wasEnabled = GUI.enabled;
            GUI.enabled = unlocked;
            if (GUI.Button(new Rect(x + width - 134, rowY + 34, 92, 44), unlocked ? "LAUNCH >" : "LOCKED", button))
                StartCircuit(i);
            GUI.enabled = wasEnabled;
        }

        if (GUI.Button(new Rect(x + 28, y + 724, width - 56, 42), "BACK TO TRACK   /   M", button))
            CloseSelector();
    }

    public void OpenSelector()
    {
        if (selecting) return;
        pausedBeforeSelector = paused;
        selecting = true;
        Time.timeScale = 0f;
    }

    public void CloseSelector()
    {
        selecting = false;
        paused = pausedBeforeSelector;
        Time.timeScale = paused ? 0f : 1f;
    }

    public void StartCircuit(int index)
    {
        if (!TurboLevels.SelectCircuit(index)) return;
        FindObjectOfType<ProceduralTrackGenerator>()?.Rebuild();
        FindObjectOfType<TurboWorld>()?.ApplyCircuit();
        selecting = paused = pausedBeforeSelector = false;
        resultSaved = newBest = false;
        elapsed = 0f;
        race?.ResetRace();
        Time.timeScale = 1f;
    }

    public void CompleteRace()
    {
        if (resultSaved || race?.player == null) return;
        finishPlace = race.GetPlayerPlace();
        newBest = TurboLevels.RecordFinish(TurboLevels.CurrentCircuitIndex, elapsed, finishPlace);
        resultSaved = true;
    }

    public void Restart()
    {
        if (selecting) return;
        Time.timeScale = 1f;
        paused = false;
        resultSaved = newBest = false;
        elapsed = 0f;
        race?.ResetRace();
    }

    void TogglePause()
    {
        if (selecting || race?.player == null || race.player.finished) return;
        paused = !paused;
        Time.timeScale = paused ? 0f : 1f;
    }

    Rect[] TouchRects()
    {
        float scale = Screen.height / 900f;
        Rect safe = Screen.safeArea;
        float left = Mathf.Max(28f, safe.xMin / scale + 16f);
        float right = Mathf.Min(Screen.width / scale - 28f, safe.xMax / scale - 16f);
        float bottom = Mathf.Min(872f, (Screen.height - safe.yMin) / scale - 16f);
        float size = Mathf.Min(88f, (right - left - 48f) / 4f);
        float y = bottom - size;
        return new[] {
            new Rect(left, y, size, size),
            new Rect(left + size + 12f, y, size, size),
            new Rect(right - size * 2f - 12f, y, size, size),
            new Rect(right - size, y, size, size)
        };
    }

    void UpdateTouchInput()
    {
        if (race?.player == null) return;
        var p = race.player;
        p.touchLeft = p.touchRight = p.touchBoost = p.touchBrake = false;
        if (selecting || paused || p.finished || !(Application.isMobilePlatform || Input.touchSupported)) return;
        Rect[] controls = TouchRects();
        float scale = Screen.height / 900f;
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch t = Input.GetTouch(i);
            if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) continue;
            Vector2 pt = new Vector2(t.position.x, Screen.height - t.position.y) / scale;
            p.touchLeft |= controls[0].Contains(pt);
            p.touchRight |= controls[1].Contains(pt);
            if (controls[2].Contains(pt))
            {
                if (p.hasWeapon && t.phase == TouchPhase.Began) p.touchFire = true;
                else p.touchBrake = true;
            }
            p.touchBoost |= controls[3].Contains(pt);
        }
    }

    void TouchButton(Rect rect, string text, bool held)
    {
        Color before = GUI.backgroundColor;
        GUI.backgroundColor = held ? TurboArt.Accent : Color.white;
        GUI.Box(rect, text, button);
        GUI.backgroundColor = before;
    }

    void DrawPanel(Rect rect) => GUI.DrawTexture(rect, panel);
    void Fill(Rect rect, Color color)
    {
        Color before = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = before;
    }
    void Text(Rect rect, string text, int size, Color color)
    {
        label.fontSize = size;
        label.normal.textColor = color;
        GUI.Label(rect, text, label);
    }
    static string FormatTime(float time)
    {
        int hundredths = Mathf.FloorToInt(time * 100f);
        return (hundredths / 6000).ToString("00") + ":" + (hundredths / 100 % 60).ToString("00") + "." + (hundredths % 100).ToString("00");
    }
}
