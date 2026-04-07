import { StatusBar } from 'expo-status-bar';
import { useMemo, useState } from 'react';
import { Button, ScrollView, StyleSheet, Text, View } from 'react-native';
import {
  parseUserCardGroupList,
  WordkiApiError,
  WordkiBackendService,
  type UserCardGroup,
} from '@wordki/shared';

const mockGroupsJson: unknown = [
  {
    id: 1,
    name: 'Demo (mock)',
    frontSideType: 'EN',
    backSideType: 'PL',
    cardCount: 12,
  },
];

/** Na emulatorze Android host to zwykle 10.0.2.2 zamiast localhost */
const DEFAULT_BFF = 'http://10.0.2.2:5129';

export default function App() {
  const mockGroups = useMemo(
    () => parseUserCardGroupList(mockGroupsJson),
    [],
  );

  const [liveGroups, setLiveGroups] = useState<UserCardGroup[] | null>(null);
  const [liveError, setLiveError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function loadFromBff() {
    const baseUrl = process.env.EXPO_PUBLIC_BFF_BASE_URL ?? DEFAULT_BFF;
    const userId = process.env.EXPO_PUBLIC_DEV_USER_ID ?? '';
    if (!userId.trim()) {
      setLiveError(
        'Ustaw EXPO_PUBLIC_DEV_USER_ID (Guid z cards.users), np. w .env w apps/mobile',
      );
      return;
    }
    setLoading(true);
    setLiveError(null);
    try {
      const api = new WordkiBackendService(baseUrl);
      const data = await api.getUserCardGroups(userId);
      setLiveGroups(data);
    } catch (e) {
      setLiveGroups(null);
      if (e instanceof WordkiApiError) {
        setLiveError(
          e.errors[0]?.message ?? e.message ?? `HTTP ${e.status}`,
        );
      } else {
        setLiveError(e instanceof Error ? e.message : 'Unknown error');
      }
    } finally {
      setLoading(false);
    }
  }

  return (
    <View style={styles.outer}>
      <ScrollView contentContainerStyle={styles.container}>
        <Text style={styles.title}>Wordki — mobile (Expo)</Text>
        <Text style={styles.hint}>
          Wspólny kod: @wordki/shared → Client/packages/shared
        </Text>

        <Text style={styles.section}>Model (parse, bez sieci)</Text>
        {mockGroups.map((g) => (
          <Text key={g.id} style={styles.line}>
            {g.name} — {g.frontSideType}/{g.backSideType}, {g.cardCount} kart
          </Text>
        ))}

        <Text style={styles.section}>Żywe API</Text>
        <Button
          title={loading ? 'Ładowanie…' : 'Pobierz grupy z BFF'}
          onPress={() => void loadFromBff()}
          disabled={loading}
        />
        {liveError ? <Text style={styles.error}>{liveError}</Text> : null}
        {liveGroups?.map((g) => (
          <Text key={g.id} style={styles.line}>
            {g.name} ({g.cardCount})
          </Text>
        ))}

        <StatusBar style="auto" />
      </ScrollView>
    </View>
  );
}

const styles = StyleSheet.create({
  outer: { flex: 1, backgroundColor: '#fff' },
  container: {
    padding: 20,
    paddingTop: 48,
  },
  title: { fontSize: 20, fontWeight: '600', marginBottom: 8 },
  hint: { color: '#555', fontSize: 13, marginBottom: 16 },
  section: { fontWeight: '600', marginTop: 16, marginBottom: 8 },
  line: { fontSize: 15, marginBottom: 4 },
  error: { color: '#b00020', marginTop: 8 },
});
