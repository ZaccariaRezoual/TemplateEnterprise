import { flushPromises, mount } from "@vue/test-utils";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { provideSiteApi } from "../api/site.api";
import ContactForm from "./ContactForm.vue";

/** What the form hands to the SDK. */
interface SubmittedRequest {
  body: Record<string, unknown>;
}

/**
 * SDK stub recording what the form submitted.
 *
 * Typed rather than cast at the call site: a cast would let the assertions
 * keep compiling after the payload's shape changed, which is the one thing
 * this test exists to notice.
 */
function mockApi(response: { status: number; body?: unknown } = { status: 202 }) {
  const post = vi.fn(async (_path: string, _request: SubmittedRequest) =>
    response.status >= 400
      ? {
          error: response.body ?? { status: response.status, title: "Failed" },
          response: new Response(null, { status: response.status }),
        }
      : { data: undefined, response: new Response(null, { status: response.status }) },
  );

  provideSiteApi({ POST: post } as never);
  return post;
}

describe("ContactForm", () => {
  beforeEach(() => {
    mockApi();
  });

  async function render() {
    const wrapper = mount(ContactForm);
    await flushPromises();
    return wrapper;
  }

  async function fill(wrapper: Awaited<ReturnType<typeof render>>) {
    const inputs = wrapper.findAll("input");
    await inputs[0]!.setValue("Ada");
    await inputs[1]!.setValue("ada@example.com");
    await wrapper.get("textarea").setValue("Ciao");
  }

  it("rejects an empty message before calling the API", async () => {
    const post = mockApi();
    const wrapper = await render();

    await wrapper.get("form").trigger("submit");

    expect(post).not.toHaveBeenCalled();
    expect(wrapper.text()).toContain("Inserisci il tuo nome.");
  });

  it("submits the message and confirms instead of clearing the form", async () => {
    const post = mockApi();
    const wrapper = await render();
    await fill(wrapper);

    await wrapper.get("form").trigger("submit");
    await flushPromises();

    expect(post).toHaveBeenCalledOnce();
    // An empty form after submitting reads as "nothing happened".
    expect(wrapper.find("form").exists()).toBe(false);
    expect(wrapper.get("[data-testid='contact-sent']").text()).toContain("Grazie");
  });

  it("sends the honeypot field, empty, so the server can judge it", async () => {
    const post = mockApi();
    const wrapper = await render();
    await fill(wrapper);

    await wrapper.get("form").trigger("submit");
    await flushPromises();

    const sent = post.mock.calls[0]?.[1];
    // Empty, not absent: the server must be the one deciding what an empty
    // honeypot means, and a field that only appears when filled would tell a
    // bot which one it is.
    expect(sent?.body).toMatchObject({ name: "Ada", email: "ada@example.com", website: "" });
  });

  it("keeps the honeypot out of reach of people and screen readers", async () => {
    const wrapper = await render();

    const honeypot = wrapper.get("#site-contact-website");
    expect(honeypot.attributes("tabindex")).toBe("-1");
    // Hidden from assistive technology too: a trap that catches a screen
    // reader user is not a trap, it is a bug.
    expect(honeypot.element.closest("[aria-hidden='true']")).not.toBeNull();
  });

  it("says to wait when the endpoint's rate limit answers", async () => {
    mockApi({ status: 429 });
    const wrapper = await render();
    await fill(wrapper);

    await wrapper.get("form").trigger("submit");
    await flushPromises();

    expect(wrapper.get("[role='alert']").text()).toContain("troppi messaggi");
    // The form stays, because the message is still worth sending later.
    expect(wrapper.find("form").exists()).toBe(true);
  });

  it("reports a generic failure without losing what was typed", async () => {
    mockApi({ status: 500 });
    const wrapper = await render();
    await fill(wrapper);

    await wrapper.get("form").trigger("submit");
    await flushPromises();

    expect(wrapper.get("[role='alert']").text()).toContain("Non siamo riusciti");
    expect(wrapper.get("textarea").element.value).toBe("Ciao");
  });
});
