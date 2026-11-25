import 'package:material_ui/material_ui.dart';

/// Ortak hap biçimli form alanı. `obscure: true` verilince şifre alanı olur ve
/// göster / gizle düğmesi çıkar. `prefixIcon` alanın solunda ikon gösterir;
/// `floatingLabel: false` etiketi yer tutucu gibi alanın içinde tutar.
class AppTextField extends StatefulWidget {
  const AppTextField({
    super.key,
    required this.label,
    this.controller,
    this.hint,
    this.errorText,
    this.keyboardType,
    this.textInputAction,
    this.autofillHints,
    this.validator,
    this.onFieldSubmitted,
    this.onChanged,
    this.obscure = false,
    this.enabled = true,
    this.textCapitalization = TextCapitalization.none,
    this.maxLength,
    this.prefixIcon,
    this.floatingLabel = true,
  });

  final String label;
  final TextEditingController? controller;
  final String? hint;
  final String? errorText;
  final TextInputType? keyboardType;
  final TextInputAction? textInputAction;
  final Iterable<String>? autofillHints;
  final FormFieldValidator<String>? validator;
  final ValueChanged<String>? onFieldSubmitted;
  final ValueChanged<String>? onChanged;
  final bool obscure;
  final bool enabled;
  final TextCapitalization textCapitalization;
  final int? maxLength;
  final IconData? prefixIcon;
  final bool floatingLabel;

  @override
  State<AppTextField> createState() => _AppTextFieldState();
}

class _AppTextFieldState extends State<AppTextField> {
  late bool _obscured = widget.obscure;

  @override
  Widget build(BuildContext context) {
    return TextFormField(
      controller: widget.controller,
      enabled: widget.enabled,
      obscureText: _obscured,
      enableSuggestions: !widget.obscure,
      autocorrect: !widget.obscure,
      keyboardType: widget.keyboardType,
      textInputAction: widget.textInputAction,
      textCapitalization: widget.textCapitalization,
      autofillHints: widget.autofillHints,
      validator: widget.validator,
      onFieldSubmitted: widget.onFieldSubmitted,
      onChanged: widget.onChanged,
      maxLength: widget.maxLength,
      decoration: InputDecoration(
        labelText: widget.label,
        hintText: widget.hint,
        errorText: widget.errorText,
        counterText: '',
        floatingLabelBehavior: widget.floatingLabel
            ? FloatingLabelBehavior.auto
            : FloatingLabelBehavior.never,
        prefixIcon: widget.prefixIcon == null
            ? null
            : Padding(
                padding: const EdgeInsets.only(left: 20, right: 14),
                child: Icon(widget.prefixIcon, size: 24),
              ),
        prefixIconConstraints: const BoxConstraints(
          minWidth: 58,
          minHeight: 24,
        ),
        suffixIcon: widget.obscure
            ? IconButton(
                tooltip: _obscured ? 'Şifreyi göster' : 'Şifreyi gizle',
                icon: Icon(
                  _obscured
                      ? Icons.visibility_outlined
                      : Icons.visibility_off_outlined,
                ),
                onPressed: () => setState(() => _obscured = !_obscured),
              )
            : null,
      ),
    );
  }
}
