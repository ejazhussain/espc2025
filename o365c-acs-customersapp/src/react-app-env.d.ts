/// <reference types="react" />
/// <reference types="react-dom" />

declare namespace React {
  namespace JSX {
    interface IntrinsicElements {
      [elemName: string]: any;
    }
  }
}
