import { useCallback, useEffect, useState } from 'react';
import { useService } from 'react-service-container';
import { UserManager } from '../api';

export function Login(): JSX.Element {
  const userManager = useService(UserManager);
  const [schemes, setSchemes] = useState<string[]>([]);

  const fetchSchemes = useCallback(async () => {
    setSchemes(await userManager.getAuthenticationSchemes());
  }, [userManager]);

  useEffect(() => {
    fetchSchemes();
  }, [fetchSchemes]);

  return (
    <>
      <h1>Choose an authentication scheme</h1>
      {schemes.map((scheme) => (
        <button
          key={scheme}
          type="button"
          className="btn btn-outline-primary btn-lg mx-1"
          onClick={() => userManager.signIn(scheme)}
        >
          {scheme}
        </button>
      ))}
    </>
  );
}
