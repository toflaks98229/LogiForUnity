namespace Loupedeck.LogiForUnityPlugin
{
    using System;

    // 버튼 아이콘을 코드로 그린다.
    //
    // 외부 에셋이 없으므로 모든 버튼 크기(50/80/116px)에서 선명하고, 색을 상황에 맞게 바꿀 수 있다.
    // BitmapBuilder 가 주는 것은 선, 사각형, 원, 호, 텍스트뿐이다. 폴리곤 채우기는 없어서 삼각형은 직접 채운다.
    //
    // 좌표는 0..1 정규화로 받아 캔버스 크기에 맞춘다. 그래야 아이콘 하나를 세 가지 크기에 그대로 쓴다.

    internal static class UnityIconArt
    {
        // 그릴 줄 아는 아이콘이면 true. 모르는 파라미터는 호출자가 라벨 폴백으로 처리한다.
        public static Boolean TryDraw(String actionParameter, BitmapBuilder canvas, BitmapColor color)
        {
            var s = Math.Min(canvas.Width, canvas.Height);
            var stroke = Math.Max(2f, s / 22f);

            switch (actionParameter)
            {
                case "play": Play(canvas, color); return true;
                case "pause": Pause(canvas, color); return true;
                case "step": Step(canvas, color); return true;

                case "tool.hand": PanArrows(canvas, color, stroke); return true;
                case "tool.move": MoveCross(canvas, color, stroke); return true;
                case "tool.rotate": RotateArc(canvas, color, stroke); return true;
                case "tool.scale": ScaleHandles(canvas, color, stroke); return true;
                case "tool.rect": RectHandles(canvas, color, stroke); return true;
                case "tool.transform": TransformRing(canvas, color, stroke); return true;

                case "frame": FrameBrackets(canvas, color, stroke); return true;
                case "pivot.toggle": PivotDots(canvas, color, stroke); return true;
                case "space.toggle": Globe(canvas, color, stroke); return true;

                case "undo": UndoArrow(canvas, color, stroke, mirrored: false); return true;
                case "redo": UndoArrow(canvas, color, stroke, mirrored: true); return true;
                case "duplicate": Duplicate(canvas, color, stroke); return true;
                case "gameobject.empty": Cube(canvas, color, stroke); return true;

                case "save": Floppy(canvas, color, stroke); return true;
                case "build.settings": Gear(canvas, color, stroke); return true;

                case "window.scene": WindowWithLetter(canvas, color, stroke, "S"); return true;
                case "window.game": WindowWithLetter(canvas, color, stroke, "G"); return true;
                case "window.inspector": WindowWithLetter(canvas, color, stroke, "I"); return true;
                case "window.hierarchy": WindowWithLetter(canvas, color, stroke, "H"); return true;
                case "window.project": WindowWithLetter(canvas, color, stroke, "P"); return true;
                case "window.console": WindowWithLetter(canvas, color, stroke, "C"); return true;

                case "ping": Ping(canvas, color, stroke); return true;

                case "install": InstallTray(canvas, color, stroke); return true;
                case "uninstall": Trash(canvas, color, stroke); return true;

                default: return false;
            }
        }

        // ---------------------------------------------------------------- 아이콘

        private static void Play(BitmapBuilder c, BitmapColor color) =>
            FillTriangle(c, color, 0.34f, 0.24f, 0.34f, 0.76f, 0.74f, 0.5f);

        private static void Pause(BitmapBuilder c, BitmapColor color)
        {
            FillRect(c, color, 0.32f, 0.26f, 0.13f, 0.48f);
            FillRect(c, color, 0.55f, 0.26f, 0.13f, 0.48f);
        }

        private static void Step(BitmapBuilder c, BitmapColor color)
        {
            FillTriangle(c, color, 0.30f, 0.26f, 0.30f, 0.74f, 0.62f, 0.5f);
            FillRect(c, color, 0.66f, 0.26f, 0.10f, 0.48f);
        }

        // 손 툴은 그리기 어렵고 알아보기도 힘들다. 사방 화살표(팬)가 의미를 더 잘 전달한다.
        private static void PanArrows(BitmapBuilder c, BitmapColor color, Single stroke)
        {
            Line(c, color, stroke, 0.5f, 0.18f, 0.5f, 0.82f);
            Line(c, color, stroke, 0.18f, 0.5f, 0.82f, 0.5f);
            ArrowHead(c, color, stroke, 0.5f, 0.18f, 0f, -1f);
            ArrowHead(c, color, stroke, 0.5f, 0.82f, 0f, 1f);
            ArrowHead(c, color, stroke, 0.18f, 0.5f, -1f, 0f);
            ArrowHead(c, color, stroke, 0.82f, 0.5f, 1f, 0f);
        }

        private static void MoveCross(BitmapBuilder c, BitmapColor color, Single stroke)
        {
            // Unity 이동 기즈모처럼 원점에서 뻗는 두 축.
            Line(c, color, stroke, 0.24f, 0.76f, 0.24f, 0.24f);
            Line(c, color, stroke, 0.24f, 0.76f, 0.76f, 0.76f);
            ArrowHead(c, color, stroke, 0.24f, 0.24f, 0f, -1f);
            ArrowHead(c, color, stroke, 0.76f, 0.76f, 1f, 0f);
            FillCircleN(c, color, 0.24f, 0.76f, 0.055f);
        }

        private static void RotateArc(BitmapBuilder c, BitmapColor color, Single stroke)
        {
            Arc(c, color, stroke, 0.5f, 0.5f, 0.30f, startAngle: 140f, sweepAngle: 260f);
            ArrowHead(c, color, stroke, 0.73f, 0.30f, 0.7f, 0.7f);
        }

        private static void ScaleHandles(BitmapBuilder c, BitmapColor color, Single stroke)
        {
            Line(c, color, stroke, 0.28f, 0.72f, 0.70f, 0.30f);
            FillRect(c, color, 0.20f, 0.64f, 0.16f, 0.16f);
            FillRect(c, color, 0.66f, 0.26f, 0.10f, 0.10f);
        }

        private static void RectHandles(BitmapBuilder c, BitmapColor color, Single stroke)
        {
            Rect(c, color, 0.26f, 0.30f, 0.48f, 0.40f);
            foreach (var (x, y) in new[] { (0.26f, 0.30f), (0.74f, 0.30f), (0.26f, 0.70f), (0.74f, 0.70f) })
            {
                FillCircleN(c, color, x, y, 0.055f);
            }
        }

        private static void TransformRing(BitmapBuilder c, BitmapColor color, Single stroke)
        {
            Circle(c, color, stroke, 0.5f, 0.5f, 0.30f);
            Line(c, color, stroke, 0.5f, 0.20f, 0.5f, 0.80f);
            Line(c, color, stroke, 0.20f, 0.5f, 0.80f, 0.5f);
        }

        private static void FrameBrackets(BitmapBuilder c, BitmapColor color, Single stroke)
        {
            const Single A = 0.20f;
            const Single B = 0.80f;
            const Single L = 0.16f;

            Line(c, color, stroke, A, A, A + L, A);
            Line(c, color, stroke, A, A, A, A + L);
            Line(c, color, stroke, B, A, B - L, A);
            Line(c, color, stroke, B, A, B, A + L);
            Line(c, color, stroke, A, B, A + L, B);
            Line(c, color, stroke, A, B, A, B - L);
            Line(c, color, stroke, B, B, B - L, B);
            Line(c, color, stroke, B, B, B, B - L);

            FillCircleN(c, color, 0.5f, 0.5f, 0.10f);
        }

        // 피벗 / 중심 전환: 도형의 중심점과, 한쪽으로 치우친 피벗점.
        private static void PivotDots(BitmapBuilder c, BitmapColor color, Single stroke)
        {
            Rect(c, color, 0.28f, 0.32f, 0.44f, 0.36f);
            FillCircleN(c, color, 0.50f, 0.50f, 0.055f);
            FillCircleN(c, color, 0.28f, 0.68f, 0.075f);
        }

        // 전역 / 로컬 전환: 지구본.
        private static void Globe(BitmapBuilder c, BitmapColor color, Single stroke)
        {
            Circle(c, color, stroke, 0.5f, 0.5f, 0.30f);
            Line(c, color, stroke, 0.20f, 0.5f, 0.80f, 0.5f);
            Arc(c, color, stroke, 0.5f, 0.5f, 0.30f, startAngle: 90f, sweepAngle: 180f);
            Ellipse(c, color, stroke, 0.5f, 0.5f, 0.13f, 0.30f);
        }

        private static void UndoArrow(BitmapBuilder c, BitmapColor color, Single stroke, Boolean mirrored)
        {
            var sign = mirrored ? -1f : 1f;
            var start = mirrored ? 20f : 160f;

            Arc(c, color, stroke, 0.5f, 0.56f, 0.28f, startAngle: start, sweepAngle: mirrored ? -160f : 160f);
            ArrowHead(c, color, stroke, 0.5f - (sign * 0.28f), 0.56f, -sign, -0.9f);
        }

        private static void Duplicate(BitmapBuilder c, BitmapColor color, Single stroke)
        {
            Rect(c, color, 0.22f, 0.22f, 0.40f, 0.40f);
            FillRect(c, color, 0.40f, 0.40f, 0.38f, 0.38f);
        }

        // 빈 게임오브젝트: 아이소메트릭 큐브의 외곽선.
        private static void Cube(BitmapBuilder c, BitmapColor color, Single stroke)
        {
            Line(c, color, stroke, 0.5f, 0.18f, 0.80f, 0.34f);
            Line(c, color, stroke, 0.80f, 0.34f, 0.80f, 0.66f);
            Line(c, color, stroke, 0.80f, 0.66f, 0.5f, 0.82f);
            Line(c, color, stroke, 0.5f, 0.82f, 0.20f, 0.66f);
            Line(c, color, stroke, 0.20f, 0.66f, 0.20f, 0.34f);
            Line(c, color, stroke, 0.20f, 0.34f, 0.5f, 0.18f);
            Line(c, color, stroke, 0.20f, 0.34f, 0.5f, 0.50f);
            Line(c, color, stroke, 0.80f, 0.34f, 0.5f, 0.50f);
            Line(c, color, stroke, 0.5f, 0.50f, 0.5f, 0.82f);
        }

        private static void Floppy(BitmapBuilder c, BitmapColor color, Single stroke)
        {
            Rect(c, color, 0.22f, 0.22f, 0.56f, 0.56f);
            FillRect(c, color, 0.34f, 0.22f, 0.32f, 0.18f);   // 셔터
            Rect(c, color, 0.32f, 0.52f, 0.36f, 0.26f);       // 라벨
        }

        private static void Gear(BitmapBuilder c, BitmapColor color, Single stroke)
        {
            Circle(c, color, stroke, 0.5f, 0.5f, 0.22f);
            FillCircleN(c, color, 0.5f, 0.5f, 0.07f);

            for (var i = 0; i < 8; i++)
            {
                var angle = i * Math.PI / 4d;
                var dx = (Single)Math.Cos(angle);
                var dy = (Single)Math.Sin(angle);
                Line(c, color, stroke, 0.5f + (dx * 0.24f), 0.5f + (dy * 0.24f), 0.5f + (dx * 0.34f), 0.5f + (dy * 0.34f));
            }
        }

        // 창 아이콘은 모두 같은 프레임이라 구분이 안 된다. 안에 머리글자를 넣는다.
        private static void WindowWithLetter(BitmapBuilder c, BitmapColor color, Single stroke, String letter)
        {
            Rect(c, color, 0.18f, 0.22f, 0.64f, 0.56f);
            FillRect(c, color, 0.18f, 0.22f, 0.64f, 0.12f);

            var x = (Int32)(0.18f * c.Width);
            var y = (Int32)(0.36f * c.Height);
            var w = (Int32)(0.64f * c.Width);
            var h = (Int32)(0.42f * c.Height);

            c.DrawText(letter, x, y, w, h, color, (Int32)(c.Height * 0.30f), 0, 0, null);
        }

        private static void Ping(BitmapBuilder c, BitmapColor color, Single stroke)
        {
            FillCircleN(c, color, 0.5f, 0.5f, 0.08f);
            Circle(c, color, stroke, 0.5f, 0.5f, 0.20f);
            Circle(c, color, stroke, 0.5f, 0.5f, 0.32f);
        }

        // 설치: 트레이 위로 내려오는 다운로드 화살표.
        private static void InstallTray(BitmapBuilder c, BitmapColor color, Single stroke)
        {
            Line(c, color, stroke, 0.5f, 0.20f, 0.5f, 0.56f);
            ArrowHead(c, color, stroke, 0.5f, 0.58f, 0f, 1f);
            Line(c, color, stroke, 0.26f, 0.70f, 0.26f, 0.80f);
            Line(c, color, stroke, 0.26f, 0.80f, 0.74f, 0.80f);
            Line(c, color, stroke, 0.74f, 0.80f, 0.74f, 0.70f);
        }

        // 제거: 뚜껑 달린 휴지통.
        private static void Trash(BitmapBuilder c, BitmapColor color, Single stroke)
        {
            // 뚜껑과 손잡이
            Line(c, color, stroke, 0.26f, 0.30f, 0.74f, 0.30f);
            Line(c, color, stroke, 0.42f, 0.30f, 0.44f, 0.23f);
            Line(c, color, stroke, 0.44f, 0.23f, 0.56f, 0.23f);
            Line(c, color, stroke, 0.56f, 0.23f, 0.58f, 0.30f);

            // 통
            Line(c, color, stroke, 0.32f, 0.30f, 0.36f, 0.80f);
            Line(c, color, stroke, 0.68f, 0.30f, 0.64f, 0.80f);
            Line(c, color, stroke, 0.36f, 0.80f, 0.64f, 0.80f);

            // 세로 홈
            Line(c, color, stroke, 0.45f, 0.38f, 0.46f, 0.72f);
            Line(c, color, stroke, 0.55f, 0.38f, 0.54f, 0.72f);
        }

        // ---------------------------------------------------------------- 원시 도형

        private static void Line(BitmapBuilder c, BitmapColor color, Single stroke, Single x1, Single y1, Single x2, Single y2) =>
            c.DrawLine(x1 * c.Width, y1 * c.Height, x2 * c.Width, y2 * c.Height, color, stroke);

        private static void Rect(BitmapBuilder c, BitmapColor color, Single x, Single y, Single w, Single h) =>
            c.DrawRectangle((Int32)(x * c.Width), (Int32)(y * c.Height), (Int32)(w * c.Width), (Int32)(h * c.Height), color);

        private static void FillRect(BitmapBuilder c, BitmapColor color, Single x, Single y, Single w, Single h) =>
            c.FillRectangle((Int32)(x * c.Width), (Int32)(y * c.Height), (Int32)(w * c.Width), (Int32)(h * c.Height), color);

        private static void Circle(BitmapBuilder c, BitmapColor color, Single stroke, Single cx, Single cy, Single r) =>
            c.DrawArc((Int32)(cx * c.Width), (Int32)(cy * c.Height), (Int32)(r * c.Width), 0f, 360f, color, stroke);

        private static void FillCircleN(BitmapBuilder c, BitmapColor color, Single cx, Single cy, Single r) =>
            c.FillCircle(cx * c.Width, cy * c.Height, r * c.Width, color);

        private static void Arc(BitmapBuilder c, BitmapColor color, Single stroke, Single cx, Single cy, Single r, Single startAngle, Single sweepAngle) =>
            c.DrawArc((Int32)(cx * c.Width), (Int32)(cy * c.Height), (Int32)(r * c.Width), startAngle, sweepAngle, color, stroke);

        // DrawArc 는 정원만 그린다. 지구본의 세로 자오선처럼 찌그러진 원은 선분으로 근사한다.
        private static void Ellipse(BitmapBuilder c, BitmapColor color, Single stroke, Single cx, Single cy, Single rx, Single ry)
        {
            const Int32 Segments = 32;
            var previousX = cx + rx;
            var previousY = cy;

            for (var i = 1; i <= Segments; i++)
            {
                var angle = i * 2d * Math.PI / Segments;
                var x = cx + (rx * (Single)Math.Cos(angle));
                var y = cy + (ry * (Single)Math.Sin(angle));
                Line(c, color, stroke, previousX, previousY, x, y);
                previousX = x;
                previousY = y;
            }
        }

        // (dx, dy) 방향을 향하는 화살촉. 방향 벡터는 정규화되어 있지 않아도 된다.
        private static void ArrowHead(BitmapBuilder c, BitmapColor color, Single stroke, Single x, Single y, Single dx, Single dy)
        {
            var length = (Single)Math.Sqrt((dx * dx) + (dy * dy));
            if (length < 0.0001f)
            {
                return;
            }

            dx /= length;
            dy /= length;

            const Single Size = 0.11f;
            var backX = x - (dx * Size);
            var backY = y - (dy * Size);

            // 진행 방향에 수직인 벡터.
            var perpendicularX = -dy * Size * 0.55f;
            var perpendicularY = dx * Size * 0.55f;

            Line(c, color, stroke, x, y, backX + perpendicularX, backY + perpendicularY);
            Line(c, color, stroke, x, y, backX - perpendicularX, backY - perpendicularY);
        }

        // BitmapBuilder 에는 폴리곤 채우기가 없다. 스캔라인마다 가로 사각형을 하나씩 채운다.
        private static void FillTriangle(BitmapBuilder c, BitmapColor color, Single x1, Single y1, Single x2, Single y2, Single x3, Single y3)
        {
            var px1 = x1 * c.Width;
            var py1 = y1 * c.Height;
            var px2 = x2 * c.Width;
            var py2 = y2 * c.Height;
            var px3 = x3 * c.Width;
            var py3 = y3 * c.Height;

            var minY = (Int32)Math.Floor(Math.Min(py1, Math.Min(py2, py3)));
            var maxY = (Int32)Math.Ceiling(Math.Max(py1, Math.Max(py2, py3)));

            for (var y = minY; y <= maxY; y++)
            {
                var scanline = y + 0.5f;
                var minX = Single.MaxValue;
                var maxX = Single.MinValue;

                // 세 변 각각이 이 스캔라인과 만나는 x 를 모은다.
                foreach (var (ax, ay, bx, by) in new[] { (px1, py1, px2, py2), (px2, py2, px3, py3), (px3, py3, px1, py1) })
                {
                    if ((scanline < Math.Min(ay, by)) || (scanline > Math.Max(ay, by)) || Math.Abs(by - ay) < 0.0001f)
                    {
                        continue;
                    }

                    var x = ax + ((scanline - ay) / (by - ay) * (bx - ax));
                    minX = Math.Min(minX, x);
                    maxX = Math.Max(maxX, x);
                }

                if (maxX > minX)
                {
                    c.FillRectangle((Int32)minX, y, (Int32)Math.Ceiling(maxX - minX), 1, color);
                }
            }
        }
    }
}
