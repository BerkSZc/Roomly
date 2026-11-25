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

class LoginScreen extends ConsumerStatefulWidget {
  const LoginScreen({super.key});

  @override
  ConsumerState<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends ConsumerState<LoginScreen> {
  final _formKey = GlobalKey<FormState>();
  final _email = TextEditingController();
  final _password = TextEditingController();
  bool _loading = false;
  String? _formError;

  @override
  void dispose() {
    _email.dispose();
    _password.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    setState(() => _formError = null);
    if (!_formKey.currentState!.validate()) return;

    setState(() => _loading = true);
    try {
      await ref
          .read(sessionControllerProvider.notifier)
          .login(email: _email.text.trim(), password: _password.text);
    } on ApiException catch (e) {
      if (!mounted) return;
      if (e.code == 'INVALID_CREDENTIALS' || e.code == 'ACCOUNT_LOCKED') {
        setState(() => _formError = e.message);
      } else {
        showAppSnackBar(context, e.message, isError: true);
      }
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Form(
      key: _formKey,
      child: AutofillGroup(
        child: BrandFormScaffold(
          subtitle: 'Ortak yaşamı tek bir yerden yönet ..',
          children: [
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
            ),
            const SizedBox(height: AppSpacing.lg),
            AppTextField(
              label: 'Şifre',
              floatingLabel: false,
              prefixIcon: Icons.lock_outline_rounded,
              controller: _password,
              obscure: true,
              textInputAction: TextInputAction.done,
              autofillHints: const [AutofillHints.password],
              validator: AuthValidators.loginPassword,
              onFieldSubmitted: (_) => _submit(),
              enabled: !_loading,
              errorText: _formError,
            ),
            const SizedBox(height: AppSpacing.xl),
            PrimaryButton(
              label: 'GİRİŞ YAP',
              isLoading: _loading,
              onPressed: _submit,
            ),
            const SizedBox(height: AppSpacing.md),
            AuthSwitchLink(
              question: 'Hesabın yok mu?',
              action: 'Kayıt ol',
              onPressed: _loading
                  ? null
                  : () => context.go(SessionRedirect.register),
            ),
          ],
        ),
      ),
    );
  }
}
