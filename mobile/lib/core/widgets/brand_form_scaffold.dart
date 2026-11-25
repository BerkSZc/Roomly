import 'package:material_ui/material_ui.dart';

import 'brand_header.dart';

/// Marka başlıklı, kaydırılabilir form ekranı iskeleti (giriş, kayıt).
/// Dar ekranda (< 600 dp) sıkı, geniş ekranda ferah boşluk kullanır;
/// form alanları en fazla 520 dp genişler.
class BrandFormScaffold extends StatelessWidget {
  const BrandFormScaffold({
    super.key,
    required this.subtitle,
    required this.children,
  });

  final String subtitle;
  final List<Widget> children;

  static const _compactBreakpoint = 600.0;
  static const _maxContentWidth = 780.0;
  static const _maxFieldWidth = 520.0;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: SafeArea(
        child: LayoutBuilder(
          builder: (context, constraints) {
            final compact = constraints.maxWidth < _compactBreakpoint;
            final side = compact ? 28.0 : 96.0;
            return SingleChildScrollView(
              padding: EdgeInsets.fromLTRB(side, compact ? 40 : 72, side, 48),
              child: Center(
                child: ConstrainedBox(
                  constraints: const BoxConstraints(maxWidth: _maxContentWidth),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      BrandHeader(subtitle: subtitle, compact: compact),
                      SizedBox(height: compact ? 36 : 48),
                      ConstrainedBox(
                        constraints: const BoxConstraints(
                          maxWidth: _maxFieldWidth,
                        ),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.stretch,
                          children: children,
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            );
          },
        ),
      ),
    );
  }
}
