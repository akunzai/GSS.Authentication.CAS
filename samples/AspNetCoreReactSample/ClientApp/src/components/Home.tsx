import { useEffect, useMemo, useState } from 'react';
import { Link } from 'wouter';
import { UserManager } from '../api';
import { User } from '../types';

export function Home(): React.JSX.Element {
  const userManager = useMemo(() => new UserManager(), []);
  const [user, setUser] = useState<User | null>(null);

  useEffect(() => {
    void userManager.getUser().then((nextUser) => {
      // oxlint-disable-next-line react/set-state-in-effect
      setUser(nextUser);
    });
  }, [userManager]);

  const authenticated = !!user;

  if (!authenticated) {
    return (
      <div>
        <h1>Hello, anonymous</h1>
        <Link href="/login" className="btn btn-primary">
          Login
        </Link>
      </div>
    );
  }
  return (
    <div>
      <h1>Hello, {user?.name}</h1>
      <dl>
        <dt>ID</dt>
        <dd>{user?.id}</dd>
      </dl>
      <dl>
        <dt>Email</dt>
        <dd>{user?.email}</dd>
      </dl>
      <button className="btn btn-danger" onClick={() => userManager.signOut()}>
        Logout
      </button>
    </div>
  );
}
