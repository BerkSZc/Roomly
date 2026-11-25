import 'package:material_ui/material_ui.dart';

/// Roomly bina logosu: çatılı ev ve yanında pencereli kule.
/// Renk verilmezse `colorScheme.onSurface` kullanılır.
class RoomlyLogo extends StatelessWidget {
  const RoomlyLogo({super.key, required this.size, this.color});

  final double size;
  final Color? color;

  @override
  Widget build(BuildContext context) {
    return ExcludeSemantics(
      child: SizedBox.square(
        dimension: size,
        child: CustomPaint(
          painter: _RoomlyLogoPainter(
            color: color ?? Theme.of(context).colorScheme.onSurface,
          ),
        ),
      ),
    );
  }
}

class _RoomlyLogoPainter extends CustomPainter {
  const _RoomlyLogoPainter({required this.color});

  final Color color;

  @override
  void paint(Canvas canvas, Size size) {
    final w = size.width;
    final h = size.height;

    final outline = Paint()
      ..color = color
      ..style = PaintingStyle.stroke
      ..strokeWidth = w * 0.062
      ..strokeJoin = StrokeJoin.miter
      ..strokeCap = StrokeCap.square;
    final fill = Paint()
      ..color = color
      ..style = PaintingStyle.fill;

    // Kule: sol kenarı açık, alt kenarı evin sağ duvarına bağlanır.
    canvas.drawPath(
      Path()
        ..moveTo(w * 0.44, h * 0.14)
        ..lineTo(w * 0.90, h * 0.14)
        ..lineTo(w * 0.90, h * 0.88)
        ..lineTo(w * 0.64, h * 0.88),
      outline,
    );
    for (final y in const [0.27, 0.45, 0.63]) {
      canvas.drawRect(
        Rect.fromLTWH(w * 0.72, h * y, w * 0.085, h * 0.075),
        fill,
      );
    }

    // Ev: çatı üçgeni, duvarlar ve kapı.
    final houseLeft = w * 0.09;
    final houseRight = w * 0.64;
    final apexX = w * 0.365;
    final eaveY = h * 0.50;
    final bottom = h * 0.88;

    canvas.drawPath(
      Path()
        ..moveTo(apexX, h * 0.27)
        ..lineTo(houseLeft, eaveY)
        ..lineTo(houseRight, eaveY)
        ..close(),
      outline,
    );
    canvas.drawPath(
      Path()
        ..moveTo(houseLeft + w * 0.06, eaveY)
        ..lineTo(houseLeft + w * 0.06, bottom)
        ..lineTo(houseRight - w * 0.06, bottom)
        ..lineTo(houseRight - w * 0.06, eaveY),
      outline,
    );
    final doorW = w * 0.14;
    final doorH = h * 0.24;
    canvas.drawRect(
      Rect.fromLTWH(apexX - doorW / 2, bottom - doorH, doorW, doorH),
      outline,
    );
  }

  @override
  bool shouldRepaint(covariant _RoomlyLogoPainter oldDelegate) =>
      oldDelegate.color != color;
}
