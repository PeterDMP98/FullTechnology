// FadePageTransition.cs — Transición de cambio de vista del Shell (spec §3.3 "appearance transit"): fundido de entrada de 200 ms + sutil desplazamiento vertical (6px→0) vía IPageTransition de Avalonia; animación leve (solo opacidad + 6 px, discreta por especificación F11).
using Avalonia;
using Avalonia.Animation;
using Avalonia.Media;

namespace FULLTECHNOLOGY.Presentation.Services;

// ============================================================
// Transición de aparición suave para el host de vistas del
// shell (spec §3.3: "appearance transit"), implementada sobre
// IPageTransition de Avalonia 12 (no existe PageTransition).
// Fade-in de 200 ms + slide-up sutil (6px → 0) al cambiar de
// módulo. Animación discreta (F11, spec §25): solo opacidad y
// un desplazamiento de 6 px, sin efectos llamativos.
// ============================================================
public sealed class FadePageTransition : IPageTransition
{
    public TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(200);

    public async Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
    {
        if (to is null) return;
        to.Opacity = 0;
        var translate = new TranslateTransform(0, 6);
        to.RenderTransform = translate;
        var t0 = DateTime.UtcNow;
        try
        {
            while (true)
            {
                var t = (DateTime.UtcNow - t0).TotalMilliseconds / Duration.TotalMilliseconds;
                if (t >= 1.0) break;
                t = 1d - Math.Pow(1d - t, 3); // ease-out cúbico
                to.Opacity = t;
                translate.Y = 6 * (1 - t);
                await Task.Delay(16, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Nueva transición en curso; se mantiene el estado actual.
        }
        translate.Y = 0;
        to.Opacity = 1;
    }
}