import 'package:flutter/material.dart';

/// One place for the few colours the app repeats. Intact and broken are the only two states the
/// UI ever shouts about, so they are named rather than spelled out at each call site.
class GenomeColors {
  static const intact = Color(0xFF2E7D57);
  static const broken = Color(0xFFB3261E);
  static const pathogenic = Color(0xFFB3261E);
  static const benign = Color(0xFF5F6368);
}

ThemeData buildTheme() {
  final scheme = ColorScheme.fromSeed(
    seedColor: const Color(0xFF3B5BDB),
    brightness: Brightness.light,
  );

  return ThemeData(
    useMaterial3: true,
    colorScheme: scheme,
    scaffoldBackgroundColor: const Color(0xFFF7F8FA),
    appBarTheme: AppBarTheme(
      backgroundColor: scheme.surface,
      surfaceTintColor: Colors.transparent,
      elevation: 0,
      scrolledUnderElevation: 1,
    ),
    cardTheme: CardThemeData(
      elevation: 0,
      color: Colors.white,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
        side: BorderSide(color: scheme.outlineVariant),
      ),
    ),
    inputDecorationTheme: const InputDecorationTheme(border: OutlineInputBorder()),
  );
}
