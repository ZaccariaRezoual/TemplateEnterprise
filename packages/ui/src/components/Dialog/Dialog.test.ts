import { mount } from "@vue/test-utils";
import { beforeAll, describe, expect, it, vi } from "vitest";
import Dialog from "./Dialog.vue";

// jsdom does not implement the native dialog methods; stub them so the
// component's open/close contract can still be asserted.
beforeAll(() => {
  HTMLDialogElement.prototype.showModal = vi.fn(function showModal(this: HTMLDialogElement) {
    this.open = true;
  });
  HTMLDialogElement.prototype.close = vi.fn(function close(this: HTMLDialogElement) {
    this.open = false;
    this.dispatchEvent(new Event("close"));
  });
});

function mountDialog(props: Record<string, unknown> = {}) {
  return mount(Dialog, {
    props: { title: "Delete project", ...props },
    slots: { default: "Body" },
    attachTo: document.body,
  });
}

describe("Dialog", () => {
  it("stays closed until the model says otherwise", () => {
    const wrapper = mountDialog();

    expect(wrapper.get("dialog").element.open).toBe(false);
  });

  it("opens as a modal, so the browser traps focus and blocks the page", async () => {
    const wrapper = mountDialog();

    await wrapper.setProps({ open: true });

    expect(HTMLDialogElement.prototype.showModal).toHaveBeenCalled();
    expect(wrapper.get("dialog").element.open).toBe(true);
  });

  it("labels itself with the title and description", async () => {
    const wrapper = mountDialog({ description: "This cannot be undone.", open: true });

    const dialog = wrapper.get("dialog");
    const labelledBy = dialog.attributes("aria-labelledby");
    const describedBy = dialog.attributes("aria-describedby");

    expect(wrapper.get(`#${labelledBy}`).text()).toBe("Delete project");
    expect(wrapper.get(`#${describedBy}`).text()).toBe("This cannot be undone.");
  });

  it("reports closing through the model and the close event", async () => {
    const wrapper = mountDialog({ open: true });

    wrapper.get("dialog").element.close();
    await wrapper.vm.$nextTick();

    expect(wrapper.emitted("update:open")?.at(-1)).toEqual([false]);
    expect(wrapper.emitted("close")).toHaveLength(1);
  });

  it("suppresses Escape when persistent", async () => {
    const wrapper = mountDialog({ open: true, persistent: true });
    const event = new Event("cancel", { cancelable: true });

    wrapper.get("dialog").element.dispatchEvent(event);

    expect(event.defaultPrevented).toBe(true);
  });

  it("allows Escape by default", async () => {
    const wrapper = mountDialog({ open: true });
    const event = new Event("cancel", { cancelable: true });

    wrapper.get("dialog").element.dispatchEvent(event);

    expect(event.defaultPrevented).toBe(false);
  });

  it("renders footer actions only when provided", () => {
    const withFooter = mount(Dialog, {
      props: { title: "Delete project", open: true },
      slots: { default: "Body", footer: "<button>Confirm</button>" },
    });

    expect(withFooter.find("button").exists()).toBe(true);
    expect(mountDialog({ open: true }).find("button").exists()).toBe(false);
  });
});
