import 'package:material_ui/material_ui.dart';

import '../theme/app_tokens.dart';
import 'roomly_logo.dart';

/// Logo, "ROOMLY" başlığı ve lime alt başlık. Giriş ve kayıt gibi
/// marka ekranlarının üstünde durur.
class BrandHeader extends StatelessWidget {
  const BrandHeader({super.key, required this.subtitle, this.compact = true});

  final String subtitle;
  final bool compact;

  @override
  Widget build(BuildContext context) {
    final ink = Theme.of(context).colorScheme.onSurface;
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        RoomlyLogo(size: compact ? 76 : 104, color: ink),
        SizedBox(width: compact ? 14 : 20),
        Flexible(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(
                'ROOMLY',
                style: TextStyle(
                  color: ink,
                  fontSize: compact ? 38 : 54,
                  fontWeight: FontWeight.w800,
                  height: 1,
                  letterSpacing: -0.5,
                ),
              ),
              const SizedBox(height: 6),
              Text(
                subtitle,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: TextStyle(
                  color: AppColors.limeAccent,
                  fontSize: compact ? 13 : 17.5,
                  fontWeight: FontWeight.w800,
                  letterSpacing: -0.2,
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }
}
