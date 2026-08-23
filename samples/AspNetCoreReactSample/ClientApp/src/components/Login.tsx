import { useEffect, useMemo, useState } from 'react';
import { UserManager } from '../api';

export function Login(): React.JSX.Element {
  const userManager = useMemo(() => new UserManager(), []);
  const [schemes, setSchemes] = useState<string[]>([]);

  useEffect(() => {
    void userManager.getAuthenticationSchemes().then((nextSchemes) => {
      // oxlint-disable-next-line react/set-state-in-effect
      setSchemes(nextSchemes);
    });
  }, [userManager]);

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
