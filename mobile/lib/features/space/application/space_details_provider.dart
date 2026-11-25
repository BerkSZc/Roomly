import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../data/space_repository.dart';

final spaceDetailsProvider = FutureProvider.autoDispose
    .family<SpaceDetails, ({String groupId, String userId})>(
      (ref, membership) =>
          ref.watch(spaceRepositoryProvider).get(membership.groupId),
      retry: (_, _) => null,
    );
