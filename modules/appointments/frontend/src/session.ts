/**
 * How the booking flow reaches the rest of the application.
 *
 * The last step of a booking needs an account — but this module must not
 * import the Auth module to find out whether there is one. The host answers
 * instead, exactly as it does for the public site's "way in", so the module
 * keeps working in an application assembled without authentication at all.
 */
export interface BookingSession {
  /** Whether somebody is signed in right now. */
  isAuthenticated: () => boolean;
  /**
   * Where to send someone who needs an account. It should bring them back:
   * losing the chosen slot at the sign-in screen is the point at which people
   * give up.
   */
  signInPath: () => string;
}

/**
 * What the module assumes when the host says nothing: nobody is signed in,
 * and `/login` is where accounts live.
 *
 * "Not signed in" is the safe default — the page then shows the sign-in step
 * rather than a form that would be refused by the API.
 */
const defaultSession: BookingSession = {
  isAuthenticated: () => false,
  signInPath: () => "/login",
};

let session: BookingSession = defaultSession;

/**
 * Sets the session seam. Called once by `installAppointmentsModule`.
 *
 * @param value The host's seam, or undefined for the defaults.
 */
export function provideBookingSession(value: BookingSession | undefined): void {
  // Assignment, not a conditional update: installing must fully define the
  // module's state, or a second install that kept the previous seam would
  // make the outcome depend on call order.
  session = value ?? defaultSession;
}

/**
 * Returns the configured session seam.
 *
 * @returns The host's seam, or the defaults.
 */
export function useBookingSession(): BookingSession {
  return session;
}
