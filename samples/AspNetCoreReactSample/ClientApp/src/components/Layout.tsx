import { ReactNode } from 'react';

type Props = {
  children: ReactNode;
};

export function Layout({ children }: Props): JSX.Element {
  return (
    <div className="container">
      <main role="main" className="pb-3">
        {children}
      </main>
    </div>
  );
}
