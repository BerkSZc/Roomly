import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../../core/errors/api_exception.dart';
import '../../../core/theme/app_tokens.dart';
import '../../../core/widgets/app_buttons.dart';
import '../../../core/widgets/app_snack_bar.dart';
import '../../../core/widgets/app_text_field.dart';
import '../application/session_controller.dart';
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
    final theme = Theme.of(context);
    return Scaffold(
      appBar: AppBar(
        title: const Text('Kayıt ol'),
        leading: BackButton(onPressed: () => context.go('/login')),
      ),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(AppSpacing.xl),
          child: Form(
            key: _formKey,
            child: AutofillGroup(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Text(
                    'Ev ya da yurt arkadaşlarınla ortak yaşamı yönetmek için hesap oluştur.',
                    style: theme.textTheme.bodyMedium,
                  ),
                  const SizedBox(height: AppSpacing.xl),
                  AppTextField(
                    label: 'Ad',
                    hint: 'Arkadaşlarının göreceği ad',
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
                    hint: 'En az 8 karakter, harf ve rakam',
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
                    label: 'Kayıt ol',
                    isLoading: _loading,
                    onPressed: _submit,
                  ),
                  const SizedBox(height: AppSpacing.md),
                  TextButton(
                    onPressed: _loading ? null : () => context.go('/login'),
                    child: const Text('Zaten hesabın var mı? Giriş yap'),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
