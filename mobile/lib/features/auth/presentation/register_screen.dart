import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../../core/errors/api_exception.dart';
import '../../../core/router/session_redirect.dart';
import '../../../core/theme/app_tokens.dart';
import '../../../core/widgets/app_buttons.dart';
import '../../../core/widgets/app_snack_bar.dart';
import '../../../core/widgets/app_text_field.dart';
import '../../../core/widgets/brand_form_scaffold.dart';
import '../application/session_controller.dart';
import 'auth_switch_link.dart';
import 'auth_validators.dart';

class RegisterScreen extends ConsumerStatefulWidget {
  const RegisterScreen({super.key});

  @override
  ConsumerState<RegisterScreen> createState() => _RegisterScreenState();
}

class _RegisterScreenState extends ConsumerState<RegisterScreen> {
  final _formKey = GlobalKey<FormState>();
  final _name = TextEditingController();
  final _email = TextEditingController();
  final _password = TextEditingController();
  bool _loading = false;
  Map<String, String> _serverErrors = const {};

  @override
  void dispose() {
    _name.dispose();
    _email.dispose();
    _password.dispose();
    super.dispose();
  }

  void _goToLogin() => context.go(SessionRedirect.login);

  Future<void> _submit() async {
    setState(() => _serverErrors = const {});
    if (!_formKey.currentState!.validate()) return;

    setState(() => _loading = true);
    try {
      await ref
          .read(sessionControllerProvider.notifier)
          .register(
            displayName: _name.text.trim(),
            email: _email.text.trim(),
            password: _password.text,
          );
    } on ApiException catch (e) {
      if (!mounted) return;
      if (e.code == 'EMAIL_TAKEN') {
        setState(() => _serverErrors = {'email': e.message});
      } else if (e.fieldErrors.isNotEmpty) {
        setState(() => _serverErrors = e.fieldErrors);
      } else {
        showAppSnackBar(context, e.message, isError: true);
      }
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    // AppBar'daki geri düğmesi kalktı; Android geri hareketi girişe döner.
    return PopScope(
      canPop: false,
      onPopInvokedWithResult: (didPop, _) {
        if (!didPop && !_loading) _goToLogin();
      },
      child: Form(
        key: _formKey,
        child: AutofillGroup(
          child: BrandFormScaffold(
            subtitle: 'Ev arkadaşlarınla ortak yaşamı başlat ..',
            children: [
              AppTextField(
                label: 'Ad',
                floatingLabel: false,
                prefixIcon: Icons.person_outline_rounded,
                controller: _name,
                textInputAction: TextInputAction.next,
                textCapitalization: TextCapitalization.words,
                autofillHints: const [AutofillHints.name],
                validator: AuthValidators.displayName,
                maxLength: 50,
                enabled: !_loading,
                errorText: _serverErrors['displayName'],
              ),
              const SizedBox(height: AppSpacing.lg),
              AppTextField(
                label: 'E-posta',
                floatingLabel: false,
                prefixIcon: Icons.mail_outline_rounded,
                controller: _email,
                keyboardType: TextInputType.emailAddress,
                textInputAction: TextInputAction.next,
                autofillHints: const [AutofillHints.email],
                validator: AuthValidators.email,
                enabled: !_loading,
                errorText: _serverErrors['email'],
              ),
              const SizedBox(height: AppSpacing.lg),
              AppTextField(
                label: 'Şifre',
                floatingLabel: false,
                prefixIcon: Icons.lock_outline_rounded,
                controller: _password,
                obscure: true,
                textInputAction: TextInputAction.done,
                autofillHints: const [AutofillHints.newPassword],
                validator: AuthValidators.newPassword,
                onFieldSubmitted: (_) => _submit(),
                enabled: !_loading,
                errorText: _serverErrors['password'],
              ),
              const SizedBox(height: AppSpacing.xl),
              PrimaryButton(
                label: 'KAYIT OL',
                isLoading: _loading,
                onPressed: _submit,
              ),
              const SizedBox(height: AppSpacing.md),
              AuthSwitchLink(
                question: 'Zaten hesabın var mı?',
                action: 'Giriş yap',
                onPressed: _loading ? null : _goToLogin,
              ),
            ],
          ),
        ),
      ),
    );
  }
}
