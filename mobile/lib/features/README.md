# Modüller

Her ekip modülü kendi klasöründe çalışır: `lib/features/<modul>/{data,application,presentation}`. Kaynak karar: [spec 0005](../../../docs/specs/0005-dashboard-module-registry.md).

| Klasör | Modül | Sorumlu | Menü |
|---|---|---|---|
| `auth/` | UC1 Kayıt ve giriş | Berk | |
| `invites/` | UC3 Davet koduyla katılım | Berk | Onboarding → Koda Katıl |
| `dashboard/` | Ana Sayfa, Diğer, alt bar | Berk | |
| `expenses/` | UC4 Giderler | Eda | Alt bar |
| `debts/` | UC5 Borçlar | Mehmet | Alt bar |
| `chores/` | UC7 Görevler | Öykü | Alt bar |
| `bills/` | UC6 Faturalar | Yusuf | Diğer |
| `shopping/` | UC8 Alışveriş | Başak | Diğer |
| `rules/` | UC9 Kurallar | Başak | Diğer |
| `space/` | UC2 Alan Bilgileri ve Alan Kur, UC10 Alandan Ayrıl | Berat | Diğer, Onboarding → Alan Kur |

## Placeholder'ını gerçek ekranla değiştirmek

Yalnızca kendi klasörüne dokun. Router'a ve `lib/core/`'a dokunman gerekmez.

1. Ekranını `presentation/` altına yaz (tasarım kuralları: `mobile/design.md`).
2. `<modul>_module.dart` içinde `screenBuilder` ver:

   ```dart
   final expensesModule = AppModule(
     id: 'expenses',
     title: 'Giderler',
     ucCode: 'UC4',
     owner: 'Eda',
     icon: Icons.receipt_long_outlined,
     placement: ModulePlacement.tab,
     screenBuilder: (context) => const ExpensesScreen(),
     dashboardCardBuilder: (context) => const ExpensesSummaryCard(), // isteğe bağlı
     routes: [
       GoRoute(path: 'new', builder: (context, state) => const NewExpenseScreen()), // /expenses/new
     ],
   );
   ```

   `screenBuilder` closure olduğu için `const` yerine `final` kullan.
3. Modülün rotası: alt bar modülü `/<id>`, Diğer modülü `/more/<id>`. Kendi alt rotan `context.go('/expenses/new')`.
4. Veri katmanı `system/` örneğindeki gibi: `XRepository` arayüzü, `MockXRepository`, `RemoteXRepository`, seçim `xRepositoryProvider`'da `appConfigProvider.useMock` ile.
5. Oturumdaki kullanıcı ve grubu: `ref.watch(sessionControllerProvider)` → `SessionAuthenticated(user)`, `user.group!.id`.

Yeni bir modül eklemek ya da bir modülü alt bar ile Diğer arasında taşımak `lib/core/modules/module_registry.dart`'ta (ortak dosya, ekip onayı) bir satır değişikliği ister. Alt barda tam 3 modül olur.
