using System.Collections.Generic;

public static class PerimeterPathBuilder
{
    // idx = y*w + x, y는 위로 증가
    // 반환: 시작점(0,0) 포함, 끝에 시작점은 "중복 포함하지 않음"(루프는 컨트롤러에서 처리)
    public static List<int> Build(int w, int h)
    {
        var list = new List<int>();
        if (w <= 0 || h <= 0) return list;

        // 1x1
        if (w == 1 && h == 1)
        {
            list.Add(0);
            return list;
        }

        // bottom: (0,0) -> (w-1,0)
        for (int x = 0; x < w; x++)
            list.Add(0 * w + x);

        // right: (w-1,1) -> (w-1,h-1)
        for (int y = 1; y < h; y++)
            list.Add(y * w + (w - 1));

        // top: (w-2,h-1) -> (0,h-1)
        if (h > 1)
        {
            for (int x = w - 2; x >= 0; x--)
                list.Add((h - 1) * w + x);
        }

        // left: (0,h-2) -> (0,1)
        if (w > 1)
        {
            for (int y = h - 2; y >= 1; y--)
                list.Add(y * w + 0);
        }

        return list;
    }
}
