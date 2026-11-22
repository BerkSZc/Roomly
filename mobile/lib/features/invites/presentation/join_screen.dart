import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../../core/errors/api_exception.dart';
import '../../../core/theme/app_tokens.dart';
import '../../../core/widgets/app_buttons.dart';
import '../../../core/widgets/app_snack_bar.dart';
import '../../../core/widgets/app_text_field.dart';
import '../../auth/application/session_controller.dart';
import '../data/invite_repository.dart';
import 'invite_code_format.dart';
import 'join_error_messages.dart';

/// Davet koduyla katılım ekranı (spec 0006 AC-11, AC-12).
class JoinScreen extends ConsumerStatefulWidget {
  const JoinScreen({super.key, this.initialCode});

  /// `/join?code=...` ile gelen kod (deep link, scope #11).
  final String? initialCode;

  @override
  ConsumerState<JoinScreen> createState() => _JoinScreenState();
}

class _JoinScreenState extends ConsumerState<JoinScreen> {
  final _formKey = GlobalKey<FormState>();
  late final _code = TextEditingController(text: widget.initialCode ?? '');
  bool _loading = false;
  String? _serverError;

  @override
  void dispose() {
    _code.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    setState(() => _serverError = null);
    if (!_formKey.currentState!.validate()) return;

    setState(() => _loading = true);
    try {
      final group = await ref.read(inviteRepositoryProvider).join(_code.text);
      // Snackbar kök ScaffoldMessenger'da kalır; yönlendirme bu ekranı kapatmadan önce göster.
      if (mounted) showAppSnackBar(context, '${group.name} grubuna katıldın.');
      await ref.read(sessionControllerProvider.notifier).refreshUser();
    } on ApiException catch (e) {
      if (!mounted) return;
      final message = joinErrorMessages[e.code];
      if (message != null) {
        setState(() => _serverError = message);
      } else {
        showAppSnackBar(context, e.message, isError: true);
      }
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Scaffold(
      appBar: AppBar(
        title: const Text('Koda Katıl'),
        leading: BackButton(onPressed: () => context.go('/onboarding')),
      ),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(AppSpacing.xl),
          child: Form(
            key: _formKey,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Icon(
                  Icons.vpn_key_outlined,
                  size: 56,
                  color: theme.colorScheme.primary,
                ),
                const SizedBox(height: AppSpacing.lg),
                Text(
                  'Ev arkadaşının paylaştığı davet kodunu gir.',
                  style: theme.textTheme.titleMedium,
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: AppSpacing.sm),
                Text(
                  'Kod 48 saat geçerlidir. Büyük küçük harf ve tire fark etmez.',
                  style: theme.textTheme.bodyMedium,
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: AppSpacing.xl),
                AppTextField(
                  label: 'Davet kodu',
                  hint: 'ABCD-EFGH-JKLM',
                  controller: _code,
                  textCapitalization: TextCapitalization.characters,
                  textInputAction: TextInputAction.done,
                  validator: InviteCodeFormat.validate,
                  onFieldSubmitted: (_) => _submit(),
                  enabled: !_loading,
                  errorText: _serverError,
                  maxLength: 40,
                ),
                const SizedBox(height: AppSpacing.xl),
                PrimaryButton(
                  label: 'Katıl',
                  isLoading: _loading,
                  onPressed: _submit,
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
