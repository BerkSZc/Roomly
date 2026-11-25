import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../../core/errors/api_exception.dart';
import '../../../core/theme/app_tokens.dart';
import '../../../core/widgets/app_buttons.dart';
import '../../../core/widgets/app_snack_bar.dart';
import '../../../core/widgets/app_text_field.dart';
import '../../auth/application/session_controller.dart';
import '../data/space_repository.dart';

class CreateSpaceScreen extends ConsumerStatefulWidget {
  const CreateSpaceScreen({super.key});

  @override
  ConsumerState<CreateSpaceScreen> createState() => _CreateSpaceScreenState();
}

class _CreateSpaceScreenState extends ConsumerState<CreateSpaceScreen> {
  final _form = GlobalKey<FormState>();
  final _name = TextEditingController();
  final _capacity = TextEditingController();
  SpaceType _type = SpaceType.ev;
  bool _saving = false;

  @override
  void dispose() {
    _name.dispose();
    _capacity.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (_saving || !_form.currentState!.validate()) return;
    setState(() => _saving = true);
    final session = ref.read(sessionControllerProvider.notifier);
    try {
      final space = await ref
          .read(spaceRepositoryProvider)
          .create(
            _name.text.trim(),
            _type,
            int.parse(_capacity.text.trim(), radix: 10),
          );
      if (mounted) showAppSnackBar(context, '${space.group.name} kuruldu.');
      await session.restore();
    } on ApiException catch (error) {
      if (mounted) {
        showAppSnackBar(context, error.message, isError: true);
      }
      if (error.code == 'ALREADY_IN_GROUP') await session.restore();
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Alan Kur'),
        leading: IconButton(
          icon: const BackButtonIcon(),
          tooltip: MaterialLocalizations.of(context).backButtonTooltip,
          onPressed: _saving ? null : () => context.go('/onboarding'),
        ),
      ),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.all(AppSpacing.xl),
          children: [
            Form(
              key: _form,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  AppTextField(
                    label: 'Alan adı',
                    controller: _name,
                    maxLength: 50,
                    textCapitalization: TextCapitalization.words,
                    textInputAction: TextInputAction.next,
                    enabled: !_saving,
                    validator: (value) {
                      final name = value?.trim() ?? '';
                      return name.isEmpty || name.length > 50
                          ? 'Alan adı 1–50 karakter olmalı.'
                          : null;
                    },
                  ),
                  const SizedBox(height: AppSpacing.md),
                  Text(
                    'Alan türü',
                    style: Theme.of(context).textTheme.titleMedium,
                  ),
                  const SizedBox(height: AppSpacing.sm),
                  SegmentedButton<SpaceType>(
                    segments: [
                      for (final type in SpaceType.values)
                        ButtonSegment(value: type, label: Text(type.label)),
                    ],
                    selected: {_type},
                    onSelectionChanged: _saving
                        ? null
                        : (value) {
                            setState(() => _type = value.single);
                            if (_capacity.text.trim().isNotEmpty) {
                              _form.currentState!.validate();
                            }
                          },
                  ),
                  const SizedBox(height: AppSpacing.lg),
                  AppTextField(
                    label: 'Kapasite',
                    hint: '${SpaceType.minCapacity}–${_type.maxCapacity} kişi',
                    controller: _capacity,
                    keyboardType: TextInputType.number,
                    textInputAction: TextInputAction.done,
                    enabled: !_saving,
                    validator: (value) =>
                        _type.accepts(
                          int.tryParse(value?.trim() ?? '', radix: 10),
                        )
                        ? null
                        : 'Kapasite ${SpaceType.minCapacity}–${_type.maxCapacity} kişi olmalı.',
                    onFieldSubmitted: (_) => _submit(),
                  ),
                  const SizedBox(height: AppSpacing.sm),
                  const Text('Yönetici kapasiteye dahildir.'),
                  const SizedBox(height: AppSpacing.xl),
                  PrimaryButton(
                    label: 'Yaşam Alanı Kur',
                    isLoading: _saving,
                    onPressed: _submit,
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
