import { useCallback, useEffect, useMemo, useState } from 'react';
import { UserManager } from '../api';

const MAX_RETRIES = 5;
const RETRY_DELAY_MS = 1000;

export function Login(): React.JSX.Element {
  const userManager = useMemo(() => new UserManager(), []);
  const [schemes, setSchemes] = useState<string[]>([]);
  const [loading, setLoading] = useState(true);

  const fetchSchemes = useCallback(async () => {
    setLoading(true);
    for (let attempt = 0; attempt < MAX_RETRIES; attempt++) {
      try {
        const result = await userManager.getAuthenticationSchemes();
        if (result && result.length > 0) {
          setSchemes(result);
          setLoading(false);
          return;
        }
      } catch {
        // Transient error (e.g. proxy not ready yet), retry
      }
      if (attempt < MAX_RETRIES - 1) {
        await new Promise((resolve) =>
          setTimeout(resolve, RETRY_DELAY_MS * (attempt + 1))
        );
      }
    }
    setLoading(false);
  }, [userManager]);

  useEffect(() => {
    fetchSchemes();
  }, [fetchSchemes]);

  return (
    <>
      <h1>Choose an authentication scheme</h1>
      {loading ? (
        <p>Loading...</p>
      ) : (
        schemes.map((scheme) => (
          <button
            key={scheme}
            type="button"
            className="btn btn-outline-primary btn-lg mx-1"
            onClick={() => userManager.signIn(scheme)}
          >
            {scheme}
          </button>
        ))
      )}
    </>
  );
}
