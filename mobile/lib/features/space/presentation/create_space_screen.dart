import 'package:material_ui/material_ui.dart';

import '../../../core/widgets/under_development_screen.dart';

/// UC2 · Yaşam alanı kurma (`/onboarding/create-group`) · Sorumlu: Berat.
/// Gerçek kurulum ekranı gelene kadar placeholder. Berat bu dosyayı değiştirir;
/// alan kurulunca `sessionControllerProvider.notifier.refreshUser()` çağrısı kullanıcıyı
/// dashboard'a taşır.
class CreateSpaceScreen extends StatelessWidget {
  const CreateSpaceScreen({super.key});

  @override
  Widget build(BuildContext context) => const UnderDevelopmentScreen(
    title: 'Alan Kur',
    ucCode: 'UC2',
    owner: 'Berat',
    icon: Icons.add_home_outlined,
  );
}
