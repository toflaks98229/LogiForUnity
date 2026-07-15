namespace Loupedeck.LogiForUnityPlugin
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;

    // 버튼 아이콘을 만든다.
    //
    // 우선순위는 두 단계다. src/Resources/Icons/<parameter>.png 가 임베디드 리소스로 들어 있으면 그걸 쓰고,
    // 없으면 그룹 색상과 라벨로 즉석에서 그린다. 덕분에 아이콘 없이도 플러그인이 온전히 동작하고,
    // 나중에 PNG 를 하나씩 추가하는 것만으로 점진적으로 예쁘게 만들 수 있다.
    //
    // 액션 파라미터 이름의 점은 파일 이름에서 하이픈이 된다. tool.move -> tool-move.png

    internal static class UnityIcons
    {
        private const String IconResourceFolder = "Resources.Icons";

        // 존재하는 PNG 리소스의 파일 이름. 매번 리소스를 뒤지지 않기 위해 한 번만 조사한다.
        private static readonly Lazy<HashSet<String>> AvailableIcons = new Lazy<HashSet<String>>(DiscoverIcons);

        // 렌더링 결과 캐시. Loupedeck 은 버튼을 그릴 때마다 이 코드를 부른다.
        private static readonly ConcurrentDictionary<String, BitmapImage> Cache =
            new ConcurrentDictionary<String, BitmapImage>();

        private static HashSet<String> DiscoverIcons()
        {
            var prefix = $"{typeof(UnityIcons).Namespace}.{IconResourceFolder}.";
            var icons = new HashSet<String>(StringComparer.OrdinalIgnoreCase);

            foreach (var resourceName in PluginResources.FindFiles($"^{System.Text.RegularExpressions.Regex.Escape(prefix)}.+\\.png$"))
            {
                icons.Add(resourceName.Substring(prefix.Length));
            }

            PluginLog.Verbose($"Found {icons.Count} icon resources");
            return icons;
        }

        public static BitmapImage Get(String actionParameter, String label, BitmapColor accent, PluginImageSize imageSize)
        {
            // 강조색도 키에 넣어야 한다. 브리지 연결 상태에 따라 색이 달라지는데, 색을 빼면 첫 렌더링에 얼어붙는다.
            var key = $"{actionParameter}|{imageSize}|{accent.ARGB}";
            return Cache.GetOrAdd(key, _ => Render(actionParameter, label, accent, imageSize));
        }

        private static BitmapImage Render(String actionParameter, String label, BitmapColor accent, PluginImageSize imageSize)
        {
            var fileName = $"{actionParameter?.Replace('.', '-')}.png";

            // 1) 임베디드 PNG 가 있으면 그것이 최우선이다.
            if (AvailableIcons.Value.Contains(fileName))
            {
                try
                {
                    return DrawArtwork(fileName, imageSize);
                }
                catch (Exception ex)
                {
                    // 아이콘 하나가 깨졌다고 버튼이 사라지면 안 된다. 아래 단계로 떨어진다.
                    PluginLog.Warning(ex, $"Failed to draw icon '{fileName}'");
                }
            }

            // 2) 코드로 그리는 벡터 아이콘. 모든 버튼 크기에서 선명하고 색을 상황에 맞게 바꾼다.
            var builder = new BitmapBuilder(imageSize);
            builder.Clear(new BitmapColor(24, 24, 27));

            if (UnityIconArt.TryDraw(actionParameter, builder, accent))
            {
                return builder.ToImage();
            }

            // 3) 그릴 줄 모르는 액션은 그룹 색상 띠와 라벨로.
            return DrawFallback(label, accent, imageSize);
        }

        private static BitmapImage DrawArtwork(String fileName, PluginImageSize imageSize)
        {
            var artwork = PluginResources.ReadImage(fileName);

            var builder = new BitmapBuilder(imageSize);
            builder.Clear(BitmapColor.Black);

            // 원본 비율과 무관하게 버튼을 가득 채운다. 아이콘은 정사각형으로 준비하는 것이 좋다.
            builder.DrawImage(artwork, 0, 0, builder.Width, builder.Height, BitmapRotation.None);
            return builder.ToImage();
        }

        // PNG 가 아직 없을 때. 그룹 색상 위에 라벨을 얹는다.
        private static BitmapImage DrawFallback(String label, BitmapColor accent, PluginImageSize imageSize)
        {
            var builder = new BitmapBuilder(imageSize);
            builder.Clear(new BitmapColor(24, 24, 27));

            // 상단에 그룹 색상 띠. 어느 그룹의 버튼인지 한눈에 구분된다.
            builder.FillRectangle(0, 0, builder.Width, Math.Max(2, builder.Height / 12), accent);

            builder.DrawText(WrapLabel(label), BitmapColor.White, GetFontSize(imageSize), 0, 0);
            return builder.ToImage();
        }

        private static Int32 GetFontSize(PluginImageSize imageSize) =>
            imageSize == PluginImageSize.Width60 ? 11 : 14;

        // 버튼이 좁아서 긴 라벨은 잘린다. 공백에서 줄을 나눈다.
        private static String WrapLabel(String label)
        {
            if (String.IsNullOrEmpty(label))
            {
                return String.Empty;
            }

            return label.Replace(" ", Environment.NewLine);
        }
    }
}
