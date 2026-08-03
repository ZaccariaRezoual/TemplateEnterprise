import { VueQueryPlugin } from "@tanstack/vue-query";
import { flushPromises, mount } from "@vue/test-utils";
import { describe, expect, it, vi } from "vitest";
import { provideAppointmentsApi } from "../api/appointments.api";
import MyAppointmentsPage from "./MyAppointmentsPage.vue";

function appointment(overrides: Record<string, unknown> = {}) {
  return {
    id: "00000000-0000-4000-8000-000000000001",
    serviceId: "00000000-0000-4000-8000-0000000000ff",
    serviceTitle: "Consulenza strategica",
    startUtc: "2026-09-01T08:00:00Z",
    endUtc: "2026-09-01T09:00:00Z",
    status: "Requested",
    contactPhone: "+39 333 1234567",
    customerNote: null,
    cancellationReason: null,
    canCancel: true,
    createdAtUtc: "2026-08-01T10:00:00Z",
    ...overrides,
  };
}

async function renderPage(appointments: unknown[]) {
  provideAppointmentsApi({
    GET: vi.fn(async () => ({
      data: appointments,
      response: new Response(null, { status: 200 }),
    })),
  } as never);

  const wrapper = mount(MyAppointmentsPage, {
    global: { plugins: [VueQueryPlugin] },
  });
  // Twice: the first tick resolves the query, the second lets the component
  // re-render with its data.
  await flushPromises();
  await flushPromises();
  return wrapper;
}

describe("MyAppointmentsPage", () => {
  it("calls a request a request, not a booking", async () => {
    const wrapper = await renderPage([appointment({ status: "Requested" })]);

    // The difference between a wait and a promise is the whole point of the
    // state, and a one-word badge does not carry it.
    expect(wrapper.text()).toContain("In attesa di conferma");
    expect(wrapper.text()).not.toContain("Confermato");
  });

  it("marks a confirmed appointment as confirmed", async () => {
    const wrapper = await renderPage([appointment({ status: "Confirmed" })]);

    expect(wrapper.text()).toContain("Confermato");
  });

  it("shows why an appointment was called off", async () => {
    const wrapper = await renderPage([
      appointment({
        status: "Cancelled",
        cancellationReason: "The slot was given to another booking.",
      }),
    ]);

    // "Cancelled" with no explanation is the message that makes someone stop
    // booking.
    expect(wrapper.text()).toContain("The slot was given to another booking.");
  });

  it("offers cancelling only when the server says it is still allowed", async () => {
    const allowed = await renderPage([appointment({ canCancel: true })]);
    expect(allowed.text()).toContain("Annulla");

    const past = await renderPage([appointment({ canCancel: false })]);
    expect(past.text()).not.toContain("Annulla");
  });

  it("asks for confirmation before cancelling", async () => {
    const wrapper = await renderPage([appointment({ canCancel: true })]);

    await wrapper
      .findAll("button")
      .find((button) => button.text() === "Annulla")
      ?.trigger("click");

    expect(wrapper.text()).toContain("Confermi?");
  });

  it("says so plainly when there is nothing booked", async () => {
    const wrapper = await renderPage([]);

    expect(wrapper.text()).toContain("Non hai appuntamenti");
  });
});
